using WhoSabi.Contracts.Wallets;

namespace WhoSabi.Application.Wallets;

public interface IGetWalletUseCase
{
    Task<WalletResponse> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
