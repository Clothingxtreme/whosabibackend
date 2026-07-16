namespace WhoSabi.Application.Rooms;

public interface IRoomDetailDataStore
{
    Task<RoomDetailRecord?> FindRoomDetailAsync(
        Guid userId,
        string roomId,
        CancellationToken cancellationToken = default);
}
