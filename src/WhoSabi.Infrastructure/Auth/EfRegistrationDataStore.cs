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
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(85320130)", cancellationToken);
        if (await dbContext.Users.AnyAsync(x => x.NormalizedEmail == pendingSignup.NormalizedEmail || x.Username.ToLower() == pendingSignup.Username.ToLower() || x.PhoneNumber == pendingSignup.PhoneNumber, cancellationToken) ||
            await dbContext.PendingSignups.AnyAsync(x => x.Status == PendingSignupStatus.PendingPayment && x.ExpiresAt > pendingSignup.CreatedAt && (x.NormalizedEmail == pendingSignup.NormalizedEmail || x.Username.ToLower() == pendingSignup.Username.ToLower() || x.PhoneNumber == pendingSignup.PhoneNumber), cancellationToken))
            throw new RegistrationInitiationException("IDENTIFIER_TAKEN", "Username or phone is already in use.");
        dbContext.PendingSignups.Add(pendingSignup);
        dbContext.PaymentTransactions.Add(paymentTransaction);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
