namespace WhoSabi.Domain.Users;

public sealed class User
{
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public string? AcceptedPolicyVersion { get; set; }
    public DateTimeOffset? PolicyAcceptedAt { get; set; }
    public bool IsTestAccount { get; set; }
    public string? GoogleSubject { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string AvatarLabel { get; set; } = string.Empty;

    public string ReferralCode { get; set; } = string.Empty;

    public bool HasPaidRegistrationFee { get; set; }

    public int GamesPlayed { get; set; }

    public int Wins { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }
}
