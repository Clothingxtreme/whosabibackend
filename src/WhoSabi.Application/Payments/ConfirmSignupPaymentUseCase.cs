using WhoSabi.Contracts.Payments;
using WhoSabi.Domain.Auth;
using WhoSabi.Domain.Payments;

namespace WhoSabi.Application.Payments;

public sealed class ConfirmSignupPaymentUseCase(
    ISignupPaymentActivationDataStore dataStore) : IConfirmSignupPaymentUseCase
{
    private const long StarterBonusKobo = 50_000;

    public async Task<ConfirmSignupPaymentResponse> HandleAsync(
        ConfirmSignupPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var paymentReference = NormalizeRequired(
            request.PaymentReference,
            80,
            "INVALID_PAYMENT_REFERENCE",
            "Payment reference is required.");

        var providerReference = NormalizeRequired(
            request.ProviderReference,
            150,
            "INVALID_PROVIDER_REFERENCE",
            "Provider reference is required.");

        if (request.AmountKobo <= 0)
        {
            throw new ConfirmSignupPaymentException(
                "INVALID_PAYMENT_AMOUNT",
                "Payment amount must be greater than zero.");
        }

        var record = await dataStore.FindSignupPaymentAsync(paymentReference, cancellationToken);
        if (record is null)
        {
            throw new ConfirmSignupPaymentException(
                "PAYMENT_NOT_FOUND",
                "Payment reference was not found.");
        }

        var pendingSignup = record.PendingSignup;
        var payment = record.PaymentTransaction;

        if (payment.Purpose != PaymentPurpose.SignupActivation)
        {
            throw new ConfirmSignupPaymentException(
                "INVALID_PAYMENT_PURPOSE",
                "Payment reference is not for signup activation.");
        }

        if (payment.AmountKobo != request.AmountKobo)
        {
            throw new ConfirmSignupPaymentException(
                "PAYMENT_AMOUNT_MISMATCH",
                "Payment amount does not match the expected signup amount.");
        }

        if (pendingSignup.Status == PendingSignupStatus.Completed &&
            payment.Status == PaymentStatus.Paid)
        {
            var activatedSignup = await dataStore.FindActivatedSignupAsync(
                pendingSignup.NormalizedEmail,
                cancellationToken);

            if (activatedSignup is not null)
            {
                return activatedSignup;
            }
        }

        if (pendingSignup.Status != PendingSignupStatus.PendingPayment ||
            payment.Status is not (PaymentStatus.Initiated or PaymentStatus.Pending))
        {
            throw new ConfirmSignupPaymentException(
                "INVALID_PAYMENT_STATE",
                "Signup payment cannot be completed from its current state.");
        }

        var now = DateTimeOffset.UtcNow;
        if (pendingSignup.ExpiresAt <= now || payment.ExpiresAt <= now)
        {
            throw new ConfirmSignupPaymentException(
                "PAYMENT_EXPIRED",
                "Signup payment has expired. Start registration again.");
        }

        return await dataStore.CompleteSignupPaymentAsync(
            payment.Id,
            providerReference,
            StarterBonusKobo,
            now,
            cancellationToken);
    }

    private static string NormalizeRequired(
        string value,
        int maxLength,
        string code,
        string message)
    {
        var normalized = value.Trim();

        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maxLength)
        {
            throw new ConfirmSignupPaymentException(code, message);
        }

        return normalized;
    }
}
