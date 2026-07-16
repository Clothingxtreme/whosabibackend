using WhoSabi.Contracts.Rooms;

namespace WhoSabi.Application.Rooms;

public interface IGetRoomDetailUseCase
{
    Task<RoomDetailResponse> HandleAsync(
        Guid userId,
        string roomId,
        CancellationToken cancellationToken = default);
}
