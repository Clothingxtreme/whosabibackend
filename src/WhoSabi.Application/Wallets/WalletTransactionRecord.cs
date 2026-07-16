namespace WhoSabi.Application.Wallets;

public sealed record WalletTransactionRecord(
    Guid Id,
    long AmountKobo,
    long BalanceAfterKobo,
    string Type,
    string Title,
    string Description,
    string? Reference,
    DateTimeOffset CreatedAt);
