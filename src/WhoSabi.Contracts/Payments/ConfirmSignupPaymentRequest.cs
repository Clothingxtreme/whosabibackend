namespace WhoSabi.Contracts.Payments;

public sealed record ConfirmSignupPaymentRequest(
    string PaymentReference,
    string ProviderReference,
    long AmountKobo);
