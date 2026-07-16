namespace WhoSabi.Contracts.Rooms;

public sealed record RoomQuestionResponse(
    string Id,
    string Category,
    string Prompt,
    IReadOnlyList<string> Options);
