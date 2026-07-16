using WhoSabi.Contracts.Wallets;

namespace WhoSabi.Application.Wallets;

public sealed class GetWalletUseCase(IWalletDataStore dataStore) : IGetWalletUseCase
{
    public async Task<WalletResponse> HandleAsync(
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

        return new WalletResponse(
            wallet.WalletAccountId,
            wallet.BalanceKobo,
            wallet.BalanceKobo / 100m,
            wallet.Currency,
            wallet.CreatedAt,
            wallet.UpdatedAt);
    }
}
