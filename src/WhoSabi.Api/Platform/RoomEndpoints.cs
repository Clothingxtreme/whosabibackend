using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using WhoSabi.Domain.Platform;
using WhoSabi.Infrastructure.Persistence;

namespace WhoSabi.Api.Platform;

public static class RealtimeEndpoints
{
    public sealed record QuestionRequest(string Category, string Prompt, string[] Options, int CorrectIndex);
    public static void MapRoundsAndChat(this WebApplication app)
    {
        var group = app.MapGroup("/api/rooms/{roomId}").RequireAuthorization();
        group.MapGet("/questions", async (string roomId, ClaimsPrincipal user, WhoSabiDbContext db, CancellationToken ct) => {
            await ApiSupport.Host(db, roomId, user.UserId(), ct);
            return Results.Ok(await db.RoomQuestions.Where(x => x.RoomId == roomId).OrderBy(x => x.SortOrder).ToArrayAsync(ct));
        });
        group.MapPost("/questions", async (string roomId, QuestionRequest input, ClaimsPrincipal user, WhoSabiDbContext db, CancellationToken ct) => {
            await ApiSupport.Host(db, roomId, user.UserId(), ct);
            if (input.Options is null || input.Options.Length is < 2 or > 4 || input.CorrectIndex < 0 || input.CorrectIndex >= input.Options.Length)
                throw new ApiFault(400, "INVALID_QUESTION", "Supply 2 to 4 options and a valid correct index.");
            var options = input.Options.Select(x => ApiSupport.Required(x, 200, "Option")).ToArray();
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var room = await db.Rooms.FromSqlInterpolated($"SELECT * FROM rooms WHERE \"Id\" = {roomId} FOR UPDATE").SingleAsync(ct);
            if (room.Status != WhoSabi.Domain.Rooms.RoomStatus.Live) throw new ApiFault(409, "ROOM_NOT_LIVE", "Room is closed.");
            var order = await db.RoomQuestions.Where(x => x.RoomId == roomId).Select(x => (int?)x.SortOrder).MaxAsync(ct) ?? 0;
            var question = new WhoSabi.Domain.Rooms.RoomQuestion { Id = Guid.NewGuid().ToString("N"), RoomId = roomId, Category = ApiSupport.Required(input.Category, 80, "Category"), Prompt = ApiSupport.Required(input.Prompt, 500, "Prompt"), CorrectIndex = input.CorrectIndex, SortOrder = order + 1 };
            db.Add(question); db.AddRange(options.Select((value, index) => new WhoSabi.Domain.Rooms.RoomQuestionOption { QuestionId = question.Id, Value = value, SortOrder = index }));
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Results.Ok(question);
        });
        group.MapGet("/state", async (string roomId, ClaimsPrincipal user, WhoSabiDbContext db, RoundService service, CancellationToken ct) => {
            await ApiSupport.Member(db, roomId, user.UserId(), ct); return Results.Ok(await service.Snapshot(roomId, ct));
        });
        group.MapPost("/rounds", async (string roomId, StartRoundRequest input, ClaimsPrincipal user, RoundService service, CancellationToken ct) => Results.Ok(await service.Start(roomId, user.UserId(), input, ct)));
        group.MapPost("/rounds/{id:guid}/close", async (string roomId, Guid id, ClaimsPrincipal user, WhoSabiDbContext db, RoundService service, CancellationToken ct) => {
            await ApiSupport.Host(db, roomId, user.UserId(), ct);
            if (!await db.Set<RoomRound>().AnyAsync(x => x.Id == id && x.RoomId == roomId, ct)) return Results.NotFound();
            await service.Close(id, ct); return Results.Ok(await service.Snapshot(roomId, ct));
        });
        group.MapGet("/rounds/{id:guid}/results", async (string roomId, Guid id, ClaimsPrincipal user, WhoSabiDbContext db, CancellationToken ct) => {
            await ApiSupport.Member(db, roomId, user.UserId(), ct);
            var round = await db.Set<RoomRound>().SingleOrDefaultAsync(x => x.Id == id && x.RoomId == roomId, ct) ?? throw new ApiFault(404, "ROUND_NOT_FOUND", "Round not found.");
            if (round.ClosedAt is null) throw new ApiFault(409, "ROUND_NOT_CLOSED", "Results are available after the round closes.");
            var question = await db.RoomQuestions.SingleAsync(x => x.Id == round.QuestionId, ct);
            var results = await db.RoomAnswerSubmissions.Where(x => x.RoomId == roomId && x.QuestionId == round.QuestionId)
                .Select(x => new { x.UserId, x.SelectedIndex, x.IsCorrect, x.RewardKobo }).ToArrayAsync(ct);
            return Results.Ok(new { roundId = id, question.CorrectIndex, results });
        });
        group.MapGet("/chat", async (string roomId, DateTimeOffset? before, ClaimsPrincipal user, WhoSabiDbContext db, CancellationToken ct) => {
            await ApiSupport.Member(db, roomId, user.UserId(), ct);
            var query = db.Set<ChatMessage>().AsNoTracking().Where(x => x.RoomId == roomId);
            if (before is not null) query = query.Where(x => x.CreatedAt < before);
            var messages = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Take(50).ToArrayAsync(ct);
            return Results.Ok(messages.Reverse());
        });
        group.MapPost("/chat", async (string roomId, ChatRequest input, ClaimsPrincipal user, WhoSabiDbContext db, IHubContext<RoomHub> hub, CancellationToken ct) => {
            var uid = user.UserId(); await ApiSupport.Member(db, roomId, uid, ct);
            var text = ApiSupport.Required(input.Text, 1000, "Text"); var key = ApiSupport.Required(input.ClientMessageId, 80, "ClientMessageId");
            var existing = await db.Set<ChatMessage>().SingleOrDefaultAsync(x => x.RoomId == roomId && x.UserId == uid && x.ClientMessageId == key, ct);
            if (existing is not null)
            {
                if (existing.Text != text) throw new ApiFault(409, "IDEMPOTENCY_CONFLICT", "ClientMessageId already used.");
                return Results.Ok(existing);
            }
            var message = new ChatMessage { RoomId = roomId, UserId = uid, Text = text, ClientMessageId = key };
            db.Add(message); await db.SaveChangesAsync(ct);
            await hub.Clients.Group(RoomHub.Group(roomId)).SendAsync("ChatMessage", message, ct);
            return Results.Ok(message);
        });
    }
}
