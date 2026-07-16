namespace WhoSabi.Domain.Rooms;

public sealed class RoomQuestionOption
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string QuestionId { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
