namespace WhoSabi.Contracts.Auth;

public sealed record InitiateRegistrationRequest(
    string Username,
    string Email,
    string Password,
    string? ReferralCode,
    string PaymentMethod);
