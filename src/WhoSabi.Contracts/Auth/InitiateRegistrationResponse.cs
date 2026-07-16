namespace WhoSabi.Contracts.Auth;

public sealed record InitiateRegistrationResponse(
    Guid SignupId,
    string PaymentReference,
    long AmountKobo,
    decimal Amount,
    string Currency,
    string Status,
    string? PaymentUrl);
