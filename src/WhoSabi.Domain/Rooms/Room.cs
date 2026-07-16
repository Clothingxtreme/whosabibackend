namespace WhoSabi.Domain.Rooms;

public sealed class Room
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Subtitle { get; set; } = string.Empty;

    public long PrizePoolKobo { get; set; }

    public int PlayerCount { get; set; }

    public long EntryFeeKobo { get; set; }

    public RoomStatus Status { get; set; } = RoomStatus.Scheduled;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }
}
