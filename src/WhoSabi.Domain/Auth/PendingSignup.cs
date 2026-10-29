namespace WhoSabi.Domain.Auth;

public sealed class PendingSignup
{
    public string? PhoneNumber { get; set; }
    public string? AcceptedPolicyVersion { get; set; }
    public DateTimeOffset? PolicyAcceptedAt { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? ReferralCode { get; set; }

    public PendingSignupStatus Status { get; set; } = PendingSignupStatus.PendingPayment;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }
}
