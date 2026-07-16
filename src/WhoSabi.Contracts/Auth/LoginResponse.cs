namespace WhoSabi.Contracts.Auth;

public sealed record LoginResponse(
    Guid UserId,
    string Username,
    string Email,
    long WalletBalanceKobo,
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt);
