namespace WhoSabi.Application.Rooms;

public interface ISubmitRoomAnswerDataStore
{
    Task<SubmitRoomAnswerResultRecord> SubmitAnswerAsync(
        Guid userId,
        string roomId,
        string questionId,
        int selectedIndex,
        long correctAnswerRewardKobo,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}
