namespace WhoSabi.Application.Rooms;

public sealed class RoomDetailException(
    string code,
    string message) : Exception(message)
{
    public string Code { get; } = code;
}
