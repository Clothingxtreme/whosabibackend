namespace WhoSabi.Domain.Payments;

public sealed class PaymentTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? PendingSignupId { get; set; }

    public string InternalReference { get; set; } = string.Empty;

    public string? ProviderReference { get; set; }

    public PaymentPurpose Purpose { get; set; } = PaymentPurpose.SignupActivation;

    public PaymentProvider Provider { get; set; } = PaymentProvider.Unknown;

    public PaymentStatus Status { get; set; } = PaymentStatus.Initiated;

    public long AmountKobo { get; set; }

    public string Currency { get; set; } = "NGN";

    public string PaymentMethod { get; set; } = string.Empty;

    public string? PaymentUrl { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? PaidAt { get; set; }
}
