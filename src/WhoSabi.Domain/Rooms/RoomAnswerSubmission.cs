namespace WhoSabi.Domain.Rooms;

public sealed class RoomAnswerSubmission
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string RoomId { get; set; } = string.Empty;

    public string QuestionId { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public int SelectedIndex { get; set; }

    public bool IsCorrect { get; set; }

    public long RewardKobo { get; set; }

    public Guid? WalletLedgerEntryId { get; set; }

    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
}
