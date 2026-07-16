using System.Net.Mail;
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
        var email = NormalizeEmail(request.Email);
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

    private static string NormalizeEmail(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();

        if (normalized.Length > 255 || !IsValidEmail(normalized))
        {
            throw InvalidCredentials();
        }

        return normalized;
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
            "Email or password is incorrect.");
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var parsed = new MailAddress(email);
            return parsed.Address.Equals(email, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
