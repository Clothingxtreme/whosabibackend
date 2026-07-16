namespace WhoSabi.Application.Wallets;

public interface IWalletDataStore
{
    Task<WalletRecord?> FindWalletAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WalletTransactionRecord>> FindWalletTransactionsAsync(
        Guid walletAccountId,
        CancellationToken cancellationToken = default);
}
