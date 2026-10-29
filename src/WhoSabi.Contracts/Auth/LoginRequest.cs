namespace WhoSabi.Contracts.Auth;

public sealed record LoginRequest(
    string? Email = null,
    string Password = "",
    string? Identifier = null);
