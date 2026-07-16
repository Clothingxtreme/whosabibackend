namespace WhoSabi.Application.Auth;

public interface ILoginDataStore
{
    Task<LoginUserRecord?> FindUserByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);
}
