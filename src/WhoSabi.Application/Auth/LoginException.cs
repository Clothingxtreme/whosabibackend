namespace WhoSabi.Application.Auth;

public sealed class LoginException(
    string code,
    string message) : Exception(message)
{
    public string Code { get; } = code;
}
