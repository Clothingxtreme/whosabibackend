namespace WhoSabi.Application.Rooms;

public sealed record SubmitRoomAnswerResultRecord(
    string RoomId,
    string QuestionId,
    int SelectedIndex,
    bool IsCorrect,
    long RewardKobo,
    long WalletBalanceKobo,
    Guid? WalletTransactionId,
    string Status);
