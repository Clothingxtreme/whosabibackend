using WhoSabi.Contracts.Rooms;

namespace WhoSabi.Application.Rooms;

public interface IGetLiveRoomsUseCase
{
    Task<IReadOnlyList<LiveRoomResponse>> HandleAsync(
        CancellationToken cancellationToken = default);
}
