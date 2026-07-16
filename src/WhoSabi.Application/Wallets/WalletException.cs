namespace WhoSabi.Application.Wallets;

public sealed class WalletException(
    string code,
    string message) : Exception(message)
{
    public string Code { get; } = code;
}
