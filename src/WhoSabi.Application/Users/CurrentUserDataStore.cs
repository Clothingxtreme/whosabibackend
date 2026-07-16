namespace WhoSabi.Application.Users;

public interface ICurrentUserDataStore
{
    Task<CurrentUserRecord?> FindCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
