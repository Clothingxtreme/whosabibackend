using WhoSabi.Contracts.Rooms;

namespace WhoSabi.Application.Rooms;

public interface IJoinRoomUseCase
{
    Task<JoinRoomResponse> HandleAsync(
        Guid userId,
        string roomId,
        JoinRoomRequest request,
        CancellationToken cancellationToken = default);
}
