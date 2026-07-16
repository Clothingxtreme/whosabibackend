using WhoSabi.Domain.Rooms;

namespace WhoSabi.Application.Rooms;

public interface IJoinRoomDataStore
{
    Task<JoinRoomResultRecord> JoinRoomAsync(
        Guid userId,
        string roomId,
        RoomParticipantRole role,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}
