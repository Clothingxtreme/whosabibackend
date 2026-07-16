namespace WhoSabi.Application.Rooms;

public interface ILiveRoomDataStore
{
    Task<IReadOnlyList<LiveRoomRecord>> FindLiveRoomsAsync(
        CancellationToken cancellationToken = default);
}
