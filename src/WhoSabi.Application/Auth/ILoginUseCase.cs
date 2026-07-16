using WhoSabi.Contracts.Auth;

namespace WhoSabi.Application.Auth;

public interface ILoginUseCase
{
    Task<LoginResponse> HandleAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}
