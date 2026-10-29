using Microsoft.EntityFrameworkCore;
using WhoSabi.Domain.Platform;
using WhoSabi.Domain.Wallets;
using WhoSabi.Infrastructure.Persistence;

namespace WhoSabi.Api.Platform;

public sealed record TransferRequest(string Username, long AmountKobo, string IdempotencyKey);
public sealed record SendGiftRequest(string GiftId, string Username, string IdempotencyKey);
public sealed record Gift(string Id, string Name, long PriceKobo);

public sealed class WalletService(WhoSabiDbContext db, IConfiguration config)
{
    public static readonly Gift[] Gifts = [new("clap", "Clap", 10_000), new("star", "Star", 50_000), new("crown", "Crown", 100_000)];

    public static async Task<WalletAccount> Lock(WhoSabiDbContext db, Guid userId, CancellationToken ct)
    {
        var wallet = await db.WalletAccounts.FromSqlInterpolated($"SELECT * FROM wallet_accounts WHERE \"UserId\" = {userId} FOR UPDATE").SingleOrDefaultAsync(ct)
            ?? throw new ApiFault(404, "WALLET_NOT_FOUND", "Wallet not found.");
        return wallet;
    }

    public static void Credit(WhoSabiDbContext db, WalletAccount wallet, long amount, WalletLedgerEntryType type, string title, string reference)
    {
        wallet.BalanceKobo = checked(wallet.BalanceKobo + amount);
        if (wallet.BalanceKobo < 0) throw new ApiFault(409, "INSUFFICIENT_BALANCE", "Wallet balance is insufficient.");
        wallet.UpdatedAt = DateTimeOffset.UtcNow;
        db.WalletLedgerEntries.Add(new WalletLedgerEntry {
            WalletAccountId = wallet.Id, UserId = wallet.UserId, AmountKobo = amount,
            BalanceAfterKobo = wallet.BalanceKobo, Type = type, Title = title, Description = title, Reference = reference
        });
    }

    public async Task<object> Transfer(Guid sender, string username, long amount, string key, string kind, CancellationToken ct)
    {
        if (!config.GetValue<bool>("Wallet:TransfersEnabled"))
            throw new ApiFault(503, "TRANSFERS_DISABLED", "Wallet transfers and gifts await operator approval of limits and bonus eligibility.");
        key = ApiSupport.Required(key, 80, "IdempotencyKey");
        username = ApiSupport.Required(username, 50, "Username").ToLowerInvariant();
        var limit = config.GetValue<long?>("Wallet:MaxTransferKobo") ?? 1_000_000;
        if (amount <= 0 || amount > limit) throw new ApiFault(400, "INVALID_AMOUNT", $"Amount must be between 1 and {limit} kobo.");
        var recipient = await db.Users.SingleOrDefaultAsync(x => x.Username.ToLower() == username, ct)
            ?? throw new ApiFault(404, "RECIPIENT_NOT_FOUND", "Recipient not found.");
        var senderUser = await db.Users.SingleAsync(x => x.Id == sender, ct);
        if (sender == recipient.Id) throw new ApiFault(400, "SELF_TRANSFER", "Choose another recipient.");
        if (senderUser.IsTestAccount || recipient.IsTestAccount) throw new ApiFault(403, "TEST_ACCOUNT_RESTRICTED", "Test accounts cannot transfer funds or send gifts.");
        var fingerprint = $"{kind}:{recipient.Id}:{amount}";
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Lock both wallets in a consistent order to avoid A->B / B->A deadlocks.
        var wallets = new Dictionary<Guid, WalletAccount>();
        foreach (var id in new[] { sender, recipient.Id }.Order()) wallets[id] = await Lock(db, id, ct);
        var existing = await db.Set<WalletOperation>().SingleOrDefaultAsync(x => x.UserId == sender && x.Key == key, ct);
        if (existing is not null)
        {
            if (existing.Fingerprint != fingerprint) throw new ApiFault(409, "IDEMPOTENCY_CONFLICT", "Key already used for different details.");
            await tx.CommitAsync(ct); return existing;
        }
        var dailyLimit = config.GetValue<long?>("Wallet:DailyTransferLimitKobo") ?? 5_000_000;
        var since = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var sentToday = await db.Set<WalletOperation>().Where(x => x.UserId == sender && x.CreatedAt >= since).SumAsync(x => (long?)x.AmountKobo, ct) ?? 0;
        if (sentToday > dailyLimit - amount) throw new ApiFault(409, "DAILY_LIMIT_EXCEEDED", "Daily transfer limit reached.");
        var operation = new WalletOperation { UserId = sender, RecipientId = recipient.Id, Key = key, Fingerprint = fingerprint, Kind = kind, AmountKobo = amount };
        Credit(db, wallets[sender], -amount, WalletLedgerEntryType.Debit, kind, $"OP-{operation.Id:N}-OUT");
        Credit(db, wallets[recipient.Id], amount, WalletLedgerEntryType.Credit, kind, $"OP-{operation.Id:N}-IN");
        db.Add(operation);
        db.Add(new AppNotification { UserId = recipient.Id, Title = "Wallet credit", Body = $"{senderUser.Username} sent {amount / 100m:0.00} NGN.", Link = "/wallet" });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return operation;
    }
}
