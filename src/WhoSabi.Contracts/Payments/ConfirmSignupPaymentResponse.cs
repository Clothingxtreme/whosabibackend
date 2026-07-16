namespace WhoSabi.Contracts.Payments;

public sealed record ConfirmSignupPaymentResponse(
    Guid UserId,
    Guid WalletAccountId,
    long StarterBonusKobo,
    long WalletBalanceKobo,
    string Status);
