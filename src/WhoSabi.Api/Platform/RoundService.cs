using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using WhoSabi.Domain.Platform;
using WhoSabi.Domain.Rooms;
using WhoSabi.Infrastructure.Persistence;
using System.Collections.Concurrent;

namespace WhoSabi.Api.Platform;

public sealed record StartRoundRequest(string QuestionId, int DurationSeconds = 30);
public sealed record ChatRequest(string Text, string ClientMessageId);

public sealed class RoundSchedule
{
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> rounds = new();
    public void Add(Guid id, DateTimeOffset deadline) => rounds[id] = deadline;
    public void Remove(Guid id) => rounds.TryRemove(id, out _);
    public Guid[] Due() => rounds.Where(x => x.Value <= DateTimeOffset.UtcNow).Select(x => x.Key).ToArray();
}

public sealed class RoundService(WhoSabiDbContext db, IHubContext<RoomHub> hub, RoundSchedule schedule)
{
    public async Task<object> Snapshot(string roomId, CancellationToken ct)
    {
        var round = await db.Set<RoomRound>().AsNoTracking().Where(x => x.RoomId == roomId).OrderByDescending(x => x.StartedAt).FirstOrDefaultAsync(ct);
        if (round is null) return new { roomId, serverTime = DateTimeOffset.UtcNow, status = "waiting" };
        var question = await db.RoomQuestions.SingleAsync(x => x.Id == round.QuestionId, ct);
        var options = await db.RoomQuestionOptions.Where(x => x.QuestionId == question.Id).OrderBy(x => x.SortOrder).Select(x => x.Value).ToArrayAsync(ct);
        return new { roomId, roundId = round.Id, serverTime = DateTimeOffset.UtcNow, round.StartedAt, round.EndsAt,
            status = round.ClosedAt is not null ? "closed" : round.EndsAt <= DateTimeOffset.UtcNow ? "closing" : "open",
            question = new { question.Id, question.Prompt, question.Category, options },
            correctIndex = round.ClosedAt is not null ? (int?)question.CorrectIndex : null };
    }

    public async Task<object> Start(string roomId, Guid host, StartRoundRequest input, CancellationToken ct)
    {
        await ApiSupport.Host(db, roomId, host, ct);
        if (input.DurationSeconds is < 5 or > 300) throw new ApiFault(400, "INVALID_DURATION", "Round length must be 5 to 300 seconds.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var tournamentId = await db.Set<Tournament>().Where(x => x.RoomId == roomId).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        Tournament? arena = null;
        if (tournamentId is not null)
            arena = await db.Set<Tournament>().FromSqlInterpolated($"SELECT * FROM tournaments WHERE \"Id\" = {tournamentId.Value} FOR UPDATE").SingleAsync(ct);
        var room = await db.Rooms.FromSqlInterpolated($"SELECT * FROM rooms WHERE \"Id\" = {roomId} FOR UPDATE").SingleAsync(ct);
        if (room.Status != RoomStatus.Live) throw new ApiFault(409, "ROOM_NOT_LIVE", "Room is not live.");
        if (arena is not null && (arena.Status != "open" || arena.StartsAt > DateTimeOffset.UtcNow))
            throw new ApiFault(409, "TOURNAMENT_NOT_STARTED", "Wait for the tournament start time.");
        if (await db.Set<RoomRound>().AnyAsync(x => x.RoomId == roomId && x.ClosedAt == null, ct)) throw new ApiFault(409, "ROUND_ALREADY_OPEN", "Close the current round first.");
        if (!await db.RoomQuestions.AnyAsync(x => x.Id == input.QuestionId && x.RoomId == roomId, ct)) throw new ApiFault(404, "QUESTION_NOT_FOUND", "Question not found.");
        if (await db.Set<RoomRound>().AnyAsync(x => x.RoomId == roomId && x.QuestionId == input.QuestionId, ct)) throw new ApiFault(409, "QUESTION_ALREADY_USED", "Use a fresh question.");
        var now = DateTimeOffset.UtcNow;
        var round = new RoomRound { RoomId = roomId, QuestionId = input.QuestionId, StartedAt = now, EndsAt = now.AddSeconds(input.DurationSeconds) };
        db.Add(round);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        schedule.Add(round.Id, round.EndsAt);
        var state = await Snapshot(roomId, ct);
        await hub.Clients.Group(RoomHub.Group(roomId)).SendAsync("RoundStarted", state, ct);
        return state;
    }

    public async Task Close(Guid roundId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var round = await db.Set<RoomRound>().FromSqlInterpolated($"SELECT * FROM room_rounds WHERE \"Id\" = {roundId} FOR UPDATE").SingleAsync(ct);
        if (round.ClosedAt is not null) { schedule.Remove(roundId); await tx.CommitAsync(ct); return; }
        var now = DateTimeOffset.UtcNow;
        round.ClosedAt = now;
        if (round.EndsAt > now) round.EndsAt = now;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        schedule.Remove(roundId);
        await hub.Clients.Group(RoomHub.Group(round.RoomId)).SendAsync("RoundClosed", await Snapshot(round.RoomId, ct), ct);
    }
}

public sealed class RoundClock(IServiceScopeFactory scopes, RoundSchedule schedule, ILogger<RoundClock> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        var restored = false;
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<WhoSabiDbContext>();
                if (!restored)
                {
                    var pending = await db.Set<RoomRound>().Where(x => x.ClosedAt == null).Select(x => new { x.Id, x.EndsAt }).ToArrayAsync(stoppingToken);
                    foreach (var item in pending) schedule.Add(item.Id, item.EndsAt);
                    restored = true;
                }
                // Idle rooms should not poll Neon every second and consume free compute.
                foreach (var id in schedule.Due()) await scope.ServiceProvider.GetRequiredService<RoundService>().Close(id, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception e) { logger.LogError(e, "Round closure will be retried."); }
        }
    }
}
