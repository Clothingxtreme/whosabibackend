using Microsoft.EntityFrameworkCore;
using WhoSabi.Application.Users;
using WhoSabi.Infrastructure.Persistence;

namespace WhoSabi.Infrastructure.Users;

public sealed class EfCurrentUserDataStore(WhoSabiDbContext dbContext) : ICurrentUserDataStore
{
    public async Task<CurrentUserRecord?> FindCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == userId,
            cancellationToken);

        if (user is null)
        {
            return null;
        }

        var wallet = await dbContext.WalletAccounts.SingleOrDefaultAsync(
            account => account.UserId == user.Id,
            cancellationToken);

        return new CurrentUserRecord(
            user.Id,
            user.Username,
            user.Email,
            user.AvatarLabel,
            user.ReferralCode,
            user.HasPaidRegistrationFee,
            user.GamesPlayed,
            user.Wins,
            wallet?.Id,
            wallet?.BalanceKobo,
            wallet?.Currency);
    }
}
