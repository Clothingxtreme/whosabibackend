namespace WhoSabi.Contracts.Rooms;

public sealed record SubmitRoomAnswerRequest(
    string? QuestionId,
    int SelectedIndex);
