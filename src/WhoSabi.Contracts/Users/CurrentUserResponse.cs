namespace WhoSabi.Contracts.Users;

public sealed record CurrentUserResponse(
    Guid UserId,
    string Username,
    string Email,
    string AvatarLabel,
    string ReferralCode,
    bool HasPaidRegistrationFee,
    int GamesPlayed,
    int Wins,
    Guid WalletAccountId,
    long WalletBalanceKobo,
    string Currency);
