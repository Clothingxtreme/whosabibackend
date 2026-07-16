namespace WhoSabi.Domain.Rooms;

public sealed class RoomGradientColor
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string RoomId { get; set; } = string.Empty;

    public string Hex { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
