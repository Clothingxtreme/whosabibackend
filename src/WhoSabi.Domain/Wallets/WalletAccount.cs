namespace WhoSabi.Domain.Wallets;

public sealed class WalletAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public long BalanceKobo { get; set; }

    public string Currency { get; set; } = "NGN";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }
}
