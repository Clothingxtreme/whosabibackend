using WhoSabi.Contracts.Auth;

namespace WhoSabi.Application.Auth;

public sealed class LoginUseCase(
    ILoginDataStore dataStore,
    IPasswordHashingService passwordHashingService,
    IAccessTokenService accessTokenService) : ILoginUseCase
{
    public async Task<LoginResponse> HandleAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = (request.Identifier ?? request.Email ?? "").Trim().ToLowerInvariant();
        if (email.Length is < 1 or > 255) throw InvalidCredentials();
        ValidatePasswordInput(request.Password);

        var user = await dataStore.FindUserByEmailAsync(email, cancellationToken);
        if (user is null ||
            !passwordHashingService.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw InvalidCredentials();
        }

        if (user.WalletBalanceKobo is null)
        {
            throw new LoginException(
                "ACCOUNT_INCOMPLETE",
                "Account wallet is missing. Contact support.");
        }

        var accessToken = accessTokenService.CreateAccessToken(user);

        return new LoginResponse(
            user.UserId,
            user.Username,
            user.Email,
            user.WalletBalanceKobo.Value,
            accessToken.Token,
            "Bearer",
            accessToken.ExpiresAt);
    }

    private static void ValidatePasswordInput(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw InvalidCredentials();
        }
    }

    private static LoginException InvalidCredentials()
    {
        return new LoginException(
            "INVALID_CREDENTIALS",
            "Login details are incorrect.");
    }

}
