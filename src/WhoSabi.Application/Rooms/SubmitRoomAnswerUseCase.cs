using WhoSabi.Contracts.Rooms;

namespace WhoSabi.Application.Rooms;

public sealed class SubmitRoomAnswerUseCase(ISubmitRoomAnswerDataStore dataStore)
    : ISubmitRoomAnswerUseCase
{
    private const long CorrectAnswerRewardKobo = 25_000;

    public async Task<SubmitRoomAnswerResponse> HandleAsync(
        Guid userId,
        string roomId,
        SubmitRoomAnswerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new SubmitRoomAnswerException(
                "INVALID_USER",
                "A valid authenticated user is required.");
        }

        var normalizedRoomId = roomId.Trim();
        if (string.IsNullOrWhiteSpace(normalizedRoomId) || normalizedRoomId.Length > 80)
        {
            throw new SubmitRoomAnswerException(
                "INVALID_ROOM_ID",
                "A valid room id is required.");
        }

        var normalizedQuestionId = request.QuestionId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedQuestionId) || normalizedQuestionId.Length > 80)
        {
            throw new SubmitRoomAnswerException(
                "INVALID_QUESTION_ID",
                "A valid question id is required.");
        }

        if (request.SelectedIndex is < 0 or > 3)
        {
            throw new SubmitRoomAnswerException(
                "INVALID_SELECTED_INDEX",
                "Selected index must be between 0 and 3.");
        }

        var now = DateTimeOffset.UtcNow;
        var result = await dataStore.SubmitAnswerAsync(
            userId,
            normalizedRoomId,
            normalizedQuestionId,
            request.SelectedIndex,
            CorrectAnswerRewardKobo,
            now,
            cancellationToken);

        return new SubmitRoomAnswerResponse(
            result.RoomId,
            result.QuestionId,
            result.SelectedIndex,
            result.IsCorrect,
            result.RewardKobo,
            result.RewardKobo / 100m,
            result.WalletBalanceKobo,
            result.WalletBalanceKobo / 100m,
            result.WalletTransactionId,
            result.Status);
    }
}
