namespace WhoSabi.Domain.Rooms;

public sealed class RoomQuestion
{
    public string Id { get; set; } = string.Empty;

    public string RoomId { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Prompt { get; set; } = string.Empty;

    public int CorrectIndex { get; set; }

    public int SortOrder { get; set; }
}
