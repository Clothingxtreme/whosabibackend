namespace WhoSabi.Application.Auth;

public sealed class RegistrationInitiationException(
    string code,
    string message) : Exception(message)
{
    public string Code { get; } = code;
}
