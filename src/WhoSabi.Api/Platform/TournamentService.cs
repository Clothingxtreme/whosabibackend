using Microsoft.EntityFrameworkCore;
using WhoSabi.Domain.Platform;
using WhoSabi.Domain.Rooms;
using WhoSabi.Domain.Wallets;
using WhoSabi.Infrastructure.Persistence;

namespace WhoSabi.Api.Platform;

public sealed record TournamentRequest(string Title, bool InviteOnly, long EntryFeeKobo, DateTimeOffset StartsAt);
public sealed record InviteRequest(string Username);
public sealed record ContributionRequest(long AmountKobo, string IdempotencyKey);

public sealed class TournamentService(WhoSabiDbContext db, IConfiguration config)
{
    public async Task<Tournament> Access(Guid id, Guid uid, CancellationToken ct)
    {
        var item = await db.Set<Tournament>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ApiFault(404, "TOURNAMENT_NOT_FOUND", "Tournament not found.");
        if (item.InviteOnly && item.OwnerId != uid && !await db.Set<TournamentInvite>().AnyAsync(x => x.TournamentId == id && x.UserId == uid && x.Status == "accepted", ct))
            throw new ApiFault(403, "INVITE_REQUIRED", "Accept an invitation first.");
        return item;
    }

    public async Task<Tournament> Owner(Guid id, Guid uid, CancellationToken ct)
    {
        var item = await Access(id, uid, ct);
        if (item.OwnerId != uid) throw new ApiFault(403, "OWNER_REQUIRED", "Only the tournament owner can do this.");
        return item;
    }

    public async Task<object> Enter(Guid id, Guid uid, CancellationToken ct)
    {
        await Access(id, uid, ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var item = await db.Set<Tournament>().FromSqlInterpolated($"SELECT * FROM tournaments WHERE \"Id\" = {id} FOR UPDATE").SingleAsync(ct);
        await db.Entry(item).ReloadAsync(ct);
        if (item.InviteOnly && item.OwnerId != uid && !await db.Set<TournamentInvite>().AnyAsync(x => x.TournamentId == id && x.UserId == uid && x.Status == "accepted", ct))
            throw new ApiFault(403, "INVITE_REQUIRED", "Invitation was revoked.");
        var existing = await db.Set<TournamentEntry>().SingleOrDefaultAsync(x => x.TournamentId == id && x.UserId == uid, ct);
        if (existing is not null) { await tx.CommitAsync(ct); return existing; }
        if (item.Status != "open" || item.StartsAt <= DateTimeOffset.UtcNow) throw new ApiFault(409, "ENTRY_CLOSED", "Tournament entries are closed.");
        var participant = await db.RoomParticipants.SingleOrDefaultAsync(x => x.RoomId == item.RoomId && x.UserId == uid, ct);
        if (participant?.IsBanned == true) throw new ApiFault(403, "ROOM_BANNED", "The host removed you from this room.");
        if (item.EntryFeeKobo > 0)
        {
            if (uid == item.OwnerId) throw new ApiFault(403, "HOST_CANNOT_COMPETE", "Hosts know the questions and cannot compete for a paid pool.");
            await RequireMoney(uid, ct);
            var wallet = await WalletService.Lock(db, uid, ct);
            WalletService.Credit(db, wallet, -item.EntryFeeKobo, WalletLedgerEntryType.Debit, "Tournament entry", $"T-{id:N}-{uid:N}");
        }
        var entry = new TournamentEntry { TournamentId = id, UserId = uid, PaidKobo = item.EntryFeeKobo };
        db.Add(entry); item.PoolKobo = checked(item.PoolKobo + item.EntryFeeKobo);
        if (participant is null) { participant = new RoomParticipant { RoomId = item.RoomId, UserId = uid }; db.Add(participant); }
        participant.Role = RoomParticipantRole.Player; participant.EntryFeeKobo = item.EntryFeeKobo; participant.Status = RoomParticipantStatus.Active;
        var room = await db.Rooms.SingleAsync(x => x.Id == item.RoomId, ct); room.PlayerCount++; room.PrizePoolKobo = item.PoolKobo;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return entry;
    }

    private async Task RequireMoney(Guid uid, CancellationToken ct)
    {
        if (!config.GetValue<bool>("Wallet:TransfersEnabled")) throw new ApiFault(503, "PAID_ARENA_DISABLED", "Paid arena entry and contributions are disabled pending approval of money rules.");
        if (await db.Users.AnyAsync(x => x.Id == uid && x.IsTestAccount, ct)) throw new ApiFault(403, "TEST_ACCOUNT_RESTRICTED", "Test accounts cannot enter paid arenas.");
    }

    public async Task<object> Contribute(Guid id, Guid uid, ContributionRequest input, CancellationToken ct)
    {
        await Access(id, uid, ct); await RequireMoney(uid, ct);
        if (input.AmountKobo <= 0 || input.AmountKobo > 1_000_000) throw new ApiFault(400, "INVALID_AMOUNT", "Contribution must be 1 to 1000000 kobo.");
        var key = ApiSupport.Required(input.IdempotencyKey, 80, "IdempotencyKey");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var item = await db.Set<Tournament>().FromSqlInterpolated($"SELECT * FROM tournaments WHERE \"Id\" = {id} FOR UPDATE").SingleAsync(ct);
        await db.Entry(item).ReloadAsync(ct);
        var wallet = await WalletService.Lock(db, uid, ct);
        var fingerprint = $"contribution:{id}:{input.AmountKobo}";
        var existing = await db.Set<WalletOperation>().SingleOrDefaultAsync(x => x.UserId == uid && x.Key == key, ct);
        if (existing is not null)
        {
            if (existing.Fingerprint != fingerprint) throw new ApiFault(409, "IDEMPOTENCY_CONFLICT", "Key already used.");
            await tx.CommitAsync(ct); return existing;
        }
        if (item.Status != "open" || item.StartsAt <= DateTimeOffset.UtcNow) throw new ApiFault(409, "CONTRIBUTIONS_CLOSED", "Contributions are closed.");
        var entry = await db.Set<TournamentEntry>().SingleOrDefaultAsync(x => x.TournamentId == id && x.UserId == uid, ct) ?? throw new ApiFault(403, "ENTRY_REQUIRED", "Enter the tournament first.");
        var operation = new WalletOperation { UserId = uid, Key = key, Kind = "tournament_contribution", Fingerprint = fingerprint, AmountKobo = input.AmountKobo };
        WalletService.Credit(db, wallet, -input.AmountKobo, WalletLedgerEntryType.Debit, "Tournament contribution", $"OP-{operation.Id:N}");
        entry.PaidKobo = checked(entry.PaidKobo + input.AmountKobo); item.PoolKobo = checked(item.PoolKobo + input.AmountKobo);
        (await db.Rooms.SingleAsync(x => x.Id == item.RoomId, ct)).PrizePoolKobo = item.PoolKobo;
        db.Add(operation); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return operation;
    }

    public async Task<object> Settle(Guid id, Guid uid, bool cancel, CancellationToken ct)
    {
        await Owner(id, uid, ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var item = await db.Set<Tournament>().FromSqlInterpolated($"SELECT * FROM tournaments WHERE \"Id\" = {id} FOR UPDATE").SingleAsync(ct);
        await db.Entry(item).ReloadAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM rooms WHERE \"Id\" = {item.RoomId} FOR UPDATE", ct);
        var entries = await db.Set<TournamentEntry>().Where(x => x.TournamentId == id).OrderBy(x => x.UserId).ToListAsync(ct);
        if (item.Status != "open") { await tx.CommitAsync(ct); return new { item.Id, item.Status, entries }; }
        var rounds = await db.Set<RoomRound>().Where(x => x.RoomId == item.RoomId).ToListAsync(ct);
        if (rounds.Any(x => x.ClosedAt is null)) throw new ApiFault(409, "ROUND_OPEN", "Close all rounds first.");
        if (!cancel && (rounds.Count == 0 || entries.Count == 0 || DateTimeOffset.UtcNow < item.StartsAt)) throw new ApiFault(409, "TOURNAMENT_NOT_FINISHED", "Start and finish a round with entrants before settlement.");
        if (entries.Sum(x => x.PaidKobo) != item.PoolKobo) throw new ApiFault(409, "POOL_MISMATCH", "Pool requires operator reconciliation.");
        Guid[] winners = [];
        if (!cancel)
        {
            var questions = rounds.Select(x => x.QuestionId).ToArray();
            var scores = await db.RoomAnswerSubmissions.Where(x => x.RoomId == item.RoomId && questions.Contains(x.QuestionId) && x.IsCorrect)
                .GroupBy(x => x.UserId).Select(g => new { UserId = g.Key, Score = g.Count() }).ToDictionaryAsync(x => x.UserId, x => x.Score, ct);
            var high = entries.Max(x => scores.GetValueOrDefault(x.UserId));
            winners = entries.Where(x => scores.GetValueOrDefault(x.UserId) == high).Select(x => x.UserId).Order().ToArray();
        }
        foreach (var entry in entries)
        {
            var index = Array.IndexOf(winners, entry.UserId);
            var amount = cancel ? entry.PaidKobo : index < 0 ? 0 : item.PoolKobo / winners.Length + (index == 0 ? item.PoolKobo % winners.Length : 0);
            entry.PayoutKobo = amount;
            if (amount > 0) WalletService.Credit(db, await WalletService.Lock(db, entry.UserId, ct), amount, cancel ? WalletLedgerEntryType.Refund : WalletLedgerEntryType.Prize,
                cancel ? "Tournament refund" : "Tournament payout", $"SETTLE-{id:N}-{entry.UserId:N}");
            db.Add(new AppNotification { UserId = entry.UserId, Title = cancel ? "Tournament cancelled" : "Tournament settled", Body = $"Wallet credited {amount / 100m:0.00} NGN.", Link = $"/tournaments/{id}" });
        }
        item.Status = cancel ? "cancelled" : "settled"; item.SettledAt = DateTimeOffset.UtcNow;
        (await db.Rooms.SingleAsync(x => x.Id == item.RoomId, ct)).Status = RoomStatus.Closed;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return new { item.Id, item.Status, entries };
    }
}
