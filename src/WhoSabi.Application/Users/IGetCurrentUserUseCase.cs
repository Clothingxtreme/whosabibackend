using WhoSabi.Contracts.Users;

namespace WhoSabi.Application.Users;

public interface IGetCurrentUserUseCase
{
    Task<CurrentUserResponse> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
