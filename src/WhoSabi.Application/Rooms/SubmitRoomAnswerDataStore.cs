namespace WhoSabi.Application.Rooms;

public interface ISubmitRoomAnswerDataStore
{
    Task<SubmitRoomAnswerResultRecord> SubmitAnswerAsync(
        Guid userId,
        string roomId,
        string questionId,
        int selectedIndex,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}
