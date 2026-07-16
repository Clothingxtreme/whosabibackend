using WhoSabi.Contracts.Wallets;

namespace WhoSabi.Application.Wallets;

public interface IGetWalletTransactionsUseCase
{
    Task<IReadOnlyList<WalletTransactionResponse>> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
