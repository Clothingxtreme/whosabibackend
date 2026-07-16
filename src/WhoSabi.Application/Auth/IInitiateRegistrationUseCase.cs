using WhoSabi.Contracts.Auth;

namespace WhoSabi.Application.Auth;

public interface IInitiateRegistrationUseCase
{
    Task<InitiateRegistrationResponse> HandleAsync(
        InitiateRegistrationRequest request,
        CancellationToken cancellationToken = default);
}
