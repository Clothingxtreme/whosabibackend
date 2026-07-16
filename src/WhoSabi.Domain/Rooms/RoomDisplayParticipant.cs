namespace WhoSabi.Domain.Rooms;

public sealed class RoomDisplayParticipant
{
    public string Id { get; set; } = string.Empty;

    public string RoomId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Initials { get; set; } = string.Empty;

    public string GlowColorHex { get; set; } = string.Empty;

    public bool IsHostAi { get; set; }

    public int Score { get; set; }

    public int SortOrder { get; set; }
}
