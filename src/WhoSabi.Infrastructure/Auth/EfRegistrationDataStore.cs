using Microsoft.EntityFrameworkCore;
using WhoSabi.Application.Auth;
using WhoSabi.Domain.Auth;
using WhoSabi.Domain.Payments;
using WhoSabi.Infrastructure.Persistence;

namespace WhoSabi.Infrastructure.Auth;

public sealed class EfRegistrationDataStore(WhoSabiDbContext dbContext) : IRegistrationDataStore
{
    public Task<bool> HasRegisteredUserAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Users.AnyAsync(
            user => user.NormalizedEmail == normalizedEmail,
            cancellationToken);
    }

    public Task<bool> HasActivePendingSignupAsync(
        string normalizedEmail,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        return dbContext.PendingSignups.AnyAsync(
            signup =>
                signup.NormalizedEmail == normalizedEmail &&
                signup.Status == PendingSignupStatus.PendingPayment &&
                signup.ExpiresAt > now,
            cancellationToken);
    }

    public async Task AddPendingRegistrationAsync(
        PendingSignup pendingSignup,
        PaymentTransaction paymentTransaction,
        CancellationToken cancellationToken = default)
    {
        dbContext.PendingSignups.Add(pendingSignup);
        dbContext.PaymentTransactions.Add(paymentTransaction);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
