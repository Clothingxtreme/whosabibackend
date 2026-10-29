using Microsoft.EntityFrameworkCore;
using WhoSabi.Application.Rooms;
using WhoSabi.Domain.Rooms;
using WhoSabi.Domain.Platform;
using WhoSabi.Infrastructure.Persistence;

namespace WhoSabi.Infrastructure.Rooms;

public sealed class EfSubmitRoomAnswerDataStore(WhoSabiDbContext db) : ISubmitRoomAnswerDataStore
{
    public async Task<SubmitRoomAnswerResultRecord> SubmitAnswerAsync(Guid userId, string roomId, string questionId, int selectedIndex, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var ct = cancellationToken;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var round = await db.Set<RoomRound>().FromSqlInterpolated($"SELECT * FROM room_rounds WHERE \"RoomId\" = {roomId} AND \"QuestionId\" = {questionId} FOR UPDATE").SingleOrDefaultAsync(ct);
        if (round is null) throw new SubmitRoomAnswerException("ROUND_NOT_STARTED", "The host has not started this question.");
        if (!await db.RoomParticipants.AnyAsync(x => x.RoomId == roomId && x.UserId == userId && x.Role == RoomParticipantRole.Player && x.Status == RoomParticipantStatus.Active, ct))
            throw new SubmitRoomAnswerException("PLAYER_REQUIRED", "Join as a player first.");
        var wallet = await db.WalletAccounts.SingleAsync(x => x.UserId == userId, ct);
        var existing = await db.RoomAnswerSubmissions.SingleOrDefaultAsync(x => x.RoomId == roomId && x.QuestionId == questionId && x.UserId == userId, ct);
        if (existing is not null)
        {
            await tx.CommitAsync(ct);
            return new(roomId, questionId, existing.SelectedIndex, round.ClosedAt is null ? null : existing.IsCorrect, 0, wallet.BalanceKobo, null, "already_submitted");
        }
        now = DateTimeOffset.UtcNow;
        if (round.ClosedAt is not null || now >= round.EndsAt) throw new SubmitRoomAnswerException("ROUND_CLOSED", "The server deadline has passed.");
        var question = await db.RoomQuestions.SingleAsync(x => x.Id == questionId, ct);
        var options = await db.RoomQuestionOptions.CountAsync(x => x.QuestionId == questionId, ct);
        if (selectedIndex < 0 || selectedIndex >= options) throw new SubmitRoomAnswerException("INVALID_SELECTED_INDEX", "Option does not exist.");
        db.Add(new RoomAnswerSubmission { RoomId = roomId, QuestionId = questionId, UserId = userId, SelectedIndex = selectedIndex, IsCorrect = question.CorrectIndex == selectedIndex, RewardKobo = 0, SubmittedAt = now });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        // No answer leakage or unfunded per-answer credits. Tournament payout uses the funded pool.
        return new(roomId, questionId, selectedIndex, null, 0, wallet.BalanceKobo, null, "submitted");
    }
}
