namespace WhoSabi.Application.Wallets;

public sealed record WalletRecord(
    Guid WalletAccountId,
    long BalanceKobo,
    string Currency,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
