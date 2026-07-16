namespace WhoSabi.Contracts.Wallets;

public sealed record WalletResponse(
    Guid WalletAccountId,
    long BalanceKobo,
    decimal Balance,
    string Currency,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
