using WhoSabi.Contracts.Users;

namespace WhoSabi.Application.Users;

public sealed class GetCurrentUserUseCase(
    ICurrentUserDataStore dataStore) : IGetCurrentUserUseCase
{
    public async Task<CurrentUserResponse> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new CurrentUserException(
                "INVALID_USER",
                "Current user is invalid.");
        }

        var currentUser = await dataStore.FindCurrentUserAsync(userId, cancellationToken);
        if (currentUser is null)
        {
            throw new CurrentUserException(
                "USER_NOT_FOUND",
                "Current user was not found.");
        }

        if (currentUser.WalletAccountId is null ||
            currentUser.WalletBalanceKobo is null ||
            string.IsNullOrWhiteSpace(currentUser.Currency))
        {
            throw new CurrentUserException(
                "ACCOUNT_INCOMPLETE",
                "Current user's wallet is missing.");
        }

        return new CurrentUserResponse(
            currentUser.UserId,
            currentUser.Username,
            currentUser.Email,
            currentUser.AvatarLabel,
            currentUser.ReferralCode,
            currentUser.HasPaidRegistrationFee,
            currentUser.GamesPlayed,
            currentUser.Wins,
            currentUser.WalletAccountId.Value,
            currentUser.WalletBalanceKobo.Value,
            currentUser.Currency);
    }
}
