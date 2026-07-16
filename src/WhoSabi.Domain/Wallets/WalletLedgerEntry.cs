namespace WhoSabi.Domain.Wallets;

public sealed class WalletLedgerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WalletAccountId { get; set; }

    public Guid? UserId { get; set; }

    public long AmountKobo { get; set; }

    public long BalanceAfterKobo { get; set; }

    public WalletLedgerEntryType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? Reference { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
