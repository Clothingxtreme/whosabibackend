using WhoSabi.Domain.Auth;
using WhoSabi.Domain.Payments;

namespace WhoSabi.Application.Auth;

public interface IRegistrationDataStore
{
    Task<bool> HasRegisteredUserAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);

    Task<bool> HasActivePendingSignupAsync(
        string normalizedEmail,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task AddPendingRegistrationAsync(
        PendingSignup pendingSignup,
        PaymentTransaction paymentTransaction,
        CancellationToken cancellationToken = default);
}
