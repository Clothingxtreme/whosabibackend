using WhoSabi.Contracts.Rooms;

namespace WhoSabi.Application.Rooms;

public interface ISubmitRoomAnswerUseCase
{
    Task<SubmitRoomAnswerResponse> HandleAsync(
        Guid userId,
        string roomId,
        SubmitRoomAnswerRequest request,
        CancellationToken cancellationToken = default);
}
