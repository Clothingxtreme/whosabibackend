namespace WhoSabi.Application.Rooms;

public sealed class SubmitRoomAnswerException(
    string code,
    string message) : Exception(message)
{
    public string Code { get; } = code;
}
