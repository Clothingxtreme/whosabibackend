namespace WhoSabi.Contracts.Wallets;

public sealed record WalletTransactionResponse(
    Guid Id,
    long AmountKobo,
    decimal Amount,
    long BalanceAfterKobo,
    decimal BalanceAfter,
    string Type,
    string Title,
    string Description,
    string? Reference,
    DateTimeOffset CreatedAt);
