using WhoSabi.Contracts.Payments;

namespace WhoSabi.Application.Payments;

public interface ISignupPaymentActivationDataStore
{
    Task<SignupPaymentActivationRecord?> FindSignupPaymentAsync(
        string paymentReference,
        CancellationToken cancellationToken = default);

    Task<ConfirmSignupPaymentResponse?> FindActivatedSignupAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);

    Task<ConfirmSignupPaymentResponse> CompleteSignupPaymentAsync(
        Guid paymentTransactionId,
        string providerReference,
        long starterBonusKobo,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}
