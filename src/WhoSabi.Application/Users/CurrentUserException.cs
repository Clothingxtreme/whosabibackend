namespace WhoSabi.Application.Users;

public sealed class CurrentUserException(
    string code,
    string message) : Exception(message)
{
    public string Code { get; } = code;
}
