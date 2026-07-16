using WhoSabi.Contracts.Wallets;

namespace WhoSabi.Application.Wallets;

public sealed class GetWalletTransactionsUseCase(IWalletDataStore dataStore)
    : IGetWalletTransactionsUseCase
{
    public async Task<IReadOnlyList<WalletTransactionResponse>> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new WalletException(
                "INVALID_USER",
                "Current user is invalid.");
        }

        var wallet = await dataStore.FindWalletAsync(userId, cancellationToken);
        if (wallet is null)
        {
            throw new WalletException(
                "WALLET_NOT_FOUND",
                "Wallet was not found for this user.");
        }

        var transactions = await dataStore.FindWalletTransactionsAsync(
            wallet.WalletAccountId,
            cancellationToken);

        return transactions
            .Select(transaction => new WalletTransactionResponse(
                transaction.Id,
                transaction.AmountKobo,
                transaction.AmountKobo / 100m,
                transaction.BalanceAfterKobo,
                transaction.BalanceAfterKobo / 100m,
                transaction.Type,
                transaction.Title,
                transaction.Description,
                transaction.Reference,
                transaction.CreatedAt))
            .ToArray();
    }
}
