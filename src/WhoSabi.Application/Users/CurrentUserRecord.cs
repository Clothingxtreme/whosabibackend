namespace WhoSabi.Application.Users;

public sealed record CurrentUserRecord(
    Guid UserId,
    string Username,
    string Email,
    string AvatarLabel,
    string ReferralCode,
    bool HasPaidRegistrationFee,
    int GamesPlayed,
    int Wins,
    Guid? WalletAccountId,
    long? WalletBalanceKobo,
    string? Currency);
