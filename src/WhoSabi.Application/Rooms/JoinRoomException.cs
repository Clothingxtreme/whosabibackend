namespace WhoSabi.Application.Rooms;

public sealed class JoinRoomException(
    string code,
    string message) : Exception(message)
{
    public string Code { get; } = code;
}
