using Microsoft.EntityFrameworkCore;
using WhoSabi.Application.Wallets;
using WhoSabi.Infrastructure.Persistence;

namespace WhoSabi.Infrastructure.Wallets;

public sealed class EfWalletDataStore(WhoSabiDbContext dbContext) : IWalletDataStore
{
    public async Task<WalletRecord?> FindWalletAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var wallet = await dbContext.WalletAccounts.SingleOrDefaultAsync(
            account => account.UserId == userId,
            cancellationToken);

        return wallet is null
            ? null
            : new WalletRecord(
                wallet.Id,
                wallet.BalanceKobo,
                wallet.Currency,
                wallet.CreatedAt,
                wallet.UpdatedAt);
    }

    public async Task<IReadOnlyList<WalletTransactionRecord>> FindWalletTransactionsAsync(
        Guid walletAccountId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.WalletLedgerEntries
            .Where(entry => entry.WalletAccountId == walletAccountId)
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(50)
            .Select(entry => new WalletTransactionRecord(
                entry.Id,
                entry.AmountKobo,
                entry.BalanceAfterKobo,
                entry.Type.ToString(),
                entry.Title,
                entry.Description,
                entry.Reference,
                entry.CreatedAt))
            .ToArrayAsync(cancellationToken);
    }
}
