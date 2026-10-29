namespace WhoSabi.Contracts.Rooms;

public sealed record SubmitRoomAnswerResponse(
    string RoomId,
    string QuestionId,
    int SelectedIndex,
    bool? IsCorrect,
    long RewardKobo,
    decimal Reward,
    long WalletBalanceKobo,
    decimal WalletBalance,
    Guid? WalletTransactionId,
    string Status);
