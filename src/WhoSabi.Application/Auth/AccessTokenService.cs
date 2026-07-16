namespace WhoSabi.Application.Auth;

public interface IAccessTokenService
{
    AccessToken CreateAccessToken(LoginUserRecord user);
}

public sealed record AccessToken(
    string Token,
    DateTimeOffset ExpiresAt);
