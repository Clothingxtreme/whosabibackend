namespace WhoSabi.Domain.Rooms;

public sealed class RoomParticipant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string RoomId { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public RoomParticipantRole Role { get; set; }

    public RoomParticipantStatus Status { get; set; } = RoomParticipantStatus.Active;

    public long EntryFeeKobo { get; set; }

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LeftAt { get; set; }
}
