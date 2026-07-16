namespace WhoSabi.Domain.Rooms;

public sealed class RoomTag
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string RoomId { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
