using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WhoSabi.Domain.Platform;
using WhoSabi.Domain.Rooms;
using WhoSabi.Infrastructure.Persistence;

namespace WhoSabi.Api.Platform;

public static class TournamentEndpoints
{
    public static void MapTournaments(this WebApplication app)
    {
        var group = app.MapGroup("/api/tournaments").RequireAuthorization();
        group.MapGet("", async (ClaimsPrincipal user, WhoSabiDbContext db, CancellationToken ct) => {
            var uid = user.UserId();
            return Results.Ok(await db.Set<Tournament>().AsNoTracking().Where(x => !x.InviteOnly || x.OwnerId == uid || db.Set<TournamentInvite>().Any(i => i.TournamentId == x.Id && i.UserId == uid && i.Status == "accepted")).OrderByDescending(x => x.CreatedAt).Take(50).ToArrayAsync(ct));
        });
        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, TournamentService service, CancellationToken ct) => Results.Ok(await service.Access(id, user.UserId(), ct)));
        group.MapGet("/{id:guid}/entry", async (Guid id, ClaimsPrincipal user, TournamentService service, WhoSabiDbContext db, CancellationToken ct) => {
            var uid = user.UserId(); await service.Access(id, uid, ct);
            var entry = await db.Set<TournamentEntry>().AsNoTracking().SingleOrDefaultAsync(x => x.TournamentId == id && x.UserId == uid, ct);
            return entry is null ? Results.NotFound() : Results.Ok(entry);
        });
        group.MapPost("", async (TournamentRequest input, ClaimsPrincipal user, WhoSabiDbContext db, IConfiguration config, CancellationToken ct) => {
            var uid = user.UserId();
            var title = ApiSupport.Required(input.Title, 120, "Title");
            if (input.StartsAt <= DateTimeOffset.UtcNow || input.StartsAt > DateTimeOffset.UtcNow.AddDays(90) || input.EntryFeeKobo < 0 || input.EntryFeeKobo > 1_000_000) throw new ApiFault(400, "INVALID_TOURNAMENT", "Choose a future start within 90 days and an entry fee up to 1000000 kobo.");
            if (input.EntryFeeKobo > 0 && !config.GetValue<bool>("Wallet:TransfersEnabled")) throw new ApiFault(503, "PAID_ARENA_DISABLED", "Paid tournaments are disabled.");
            var room = new Room { Id = "arena-" + Guid.NewGuid().ToString("N"), Title = title, HostUserId = uid, Status = RoomStatus.Live };
            var item = new Tournament { OwnerId = uid, RoomId = room.Id, Title = title, InviteOnly = input.InviteOnly, EntryFeeKobo = input.EntryFeeKobo, StartsAt = input.StartsAt.ToUniversalTime() };
            db.Add(room); db.Add(item);
            await db.SaveChangesAsync(ct); return Results.Ok(item);
        });
        group.MapPatch("/{id:guid}", async (Guid id, TournamentRequest input, ClaimsPrincipal user, WhoSabiDbContext db, TournamentService service, CancellationToken ct) => {
            var item = await service.Owner(id, user.UserId(), ct);
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM tournaments WHERE \"Id\" = {id} FOR UPDATE", ct);
            await db.Entry(item).ReloadAsync(ct);
            if (item.Status != "open" || item.StartsAt <= DateTimeOffset.UtcNow || await db.Set<TournamentEntry>().AnyAsync(x => x.TournamentId == id, ct)) throw new ApiFault(409, "TOURNAMENT_LOCKED", "Tournament settings lock once entered or started.");
            if (input.EntryFeeKobo != item.EntryFeeKobo || input.InviteOnly != item.InviteOnly) throw new ApiFault(400, "IMMUTABLE_RULES", "Entry fee and privacy are fixed at creation.");
            if (input.StartsAt <= DateTimeOffset.UtcNow || input.StartsAt > DateTimeOffset.UtcNow.AddDays(90)) throw new ApiFault(400, "INVALID_START", "Choose a future start within 90 days.");
            item.Title = ApiSupport.Required(input.Title, 120, "Title"); item.StartsAt = input.StartsAt.ToUniversalTime();
            (await db.Rooms.SingleAsync(x => x.Id == item.RoomId, ct)).Title = item.Title;
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Results.Ok(item);
        });
        group.MapPost("/{id:guid}/enter", async (Guid id, ClaimsPrincipal user, TournamentService service, CancellationToken ct) => Results.Ok(await service.Enter(id, user.UserId(), ct)));
        group.MapPost("/{id:guid}/contributions", async (Guid id, ContributionRequest input, ClaimsPrincipal user, TournamentService service, CancellationToken ct) => Results.Ok(await service.Contribute(id, user.UserId(), input, ct)));
        group.MapPost("/{id:guid}/settle", async (Guid id, ClaimsPrincipal user, TournamentService service, CancellationToken ct) => Results.Ok(await service.Settle(id, user.UserId(), false, ct)));
        group.MapPost("/{id:guid}/cancel", async (Guid id, ClaimsPrincipal user, TournamentService service, CancellationToken ct) => Results.Ok(await service.Settle(id, user.UserId(), true, ct)));
        group.MapPost("/{id:guid}/invites", async (Guid id, InviteRequest input, ClaimsPrincipal user, WhoSabiDbContext db, TournamentService service, CancellationToken ct) => {
            var item = await service.Owner(id, user.UserId(), ct);
            if (item.Status != "open" || item.StartsAt <= DateTimeOffset.UtcNow) throw new ApiFault(409, "INVITES_CLOSED", "Invitations are closed.");
            var username = ApiSupport.Required(input.Username, 50, "Username").ToLowerInvariant();
            var recipient = await db.Users.SingleOrDefaultAsync(x => x.Username.ToLower() == username, ct) ?? throw new ApiFault(404, "USER_NOT_FOUND", "Username not found.");
            var invite = await db.Set<TournamentInvite>().SingleOrDefaultAsync(x => x.TournamentId == id && x.UserId == recipient.Id, ct);
            if (invite is null)
            {
                invite = new TournamentInvite { TournamentId = id, UserId = recipient.Id }; db.Add(invite);
                db.Add(new AppNotification { UserId = recipient.Id, Title = "Tournament invitation", Body = item.Title, Link = $"https://whosabi.ng/invites/{invite.Id}" });
                await db.SaveChangesAsync(ct);
            }
            return Results.Ok(new { invite, deepLink = $"https://whosabi.ng/invites/{invite.Id}" });
        });
        var invites = app.MapGroup("/api/invites").RequireAuthorization();
        invites.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, WhoSabiDbContext db, CancellationToken ct) => {
            var uid = user.UserId();
            var invite = await db.Set<TournamentInvite>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.UserId == uid, ct);
            return invite is null ? Results.NotFound() : Results.Ok(invite);
        });
        invites.MapPost("/{id:guid}/accept", async (Guid id, ClaimsPrincipal user, WhoSabiDbContext db, CancellationToken ct) => {
            var uid = user.UserId(); var invite = await db.Set<TournamentInvite>().SingleOrDefaultAsync(x => x.Id == id && x.UserId == uid, ct) ?? throw new ApiFault(404, "INVITE_NOT_FOUND", "Invitation not found.");
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM tournaments WHERE \"Id\" = {invite.TournamentId} FOR UPDATE", ct);
            await db.Entry(invite).ReloadAsync(ct);
            var item = await db.Set<Tournament>().SingleAsync(x => x.Id == invite.TournamentId, ct);
            if (invite.Status == "revoked" || item.Status != "open" || item.StartsAt <= DateTimeOffset.UtcNow) throw new ApiFault(409, "INVITE_UNAVAILABLE", "Invitation unavailable.");
            invite.Status = "accepted"; await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Results.Ok(invite);
        });
        invites.MapPost("/{id:guid}/revoke", async (Guid id, ClaimsPrincipal user, WhoSabiDbContext db, TournamentService service, CancellationToken ct) => {
            var invite = await db.Set<TournamentInvite>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ApiFault(404, "INVITE_NOT_FOUND", "Invitation not found.");
            await service.Owner(invite.TournamentId, user.UserId(), ct);
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM tournaments WHERE \"Id\" = {invite.TournamentId} FOR UPDATE", ct);
            await db.Entry(invite).ReloadAsync(ct);
            if (await db.Set<TournamentEntry>().AnyAsync(x => x.TournamentId == invite.TournamentId && x.UserId == invite.UserId, ct)) throw new ApiFault(409, "ENTRY_EXISTS", "Cannot revoke after entry.");
            invite.Status = "revoked"; await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Results.Ok(invite);
        });
    }
}
