namespace WhoSabi.Application.Auth;

public sealed record LoginUserRecord(
    Guid UserId,
    string Username,
    string Email,
    string NormalizedEmail,
    string PasswordHash,
    long? WalletBalanceKobo);
