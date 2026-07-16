using Microsoft.EntityFrameworkCore;
using WhoSabi.Application.Auth;
using WhoSabi.Infrastructure.Persistence;

namespace WhoSabi.Infrastructure.Auth;

public sealed class EfLoginDataStore(WhoSabiDbContext dbContext) : ILoginDataStore
{
    public async Task<LoginUserRecord?> FindUserByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.NormalizedEmail == normalizedEmail,
            cancellationToken);

        if (user is null)
        {
            return null;
        }

        var wallet = await dbContext.WalletAccounts.SingleOrDefaultAsync(
            account => account.UserId == user.Id,
            cancellationToken);

        return new LoginUserRecord(
            user.Id,
            user.Username,
            user.Email,
            user.NormalizedEmail,
            user.PasswordHash,
            wallet?.BalanceKobo);
    }
}
