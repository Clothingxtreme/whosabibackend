using System.Data;
using Microsoft.EntityFrameworkCore;
using WhoSabi.Application.Rooms;
using WhoSabi.Domain.Rooms;
using WhoSabi.Domain.Wallets;
using WhoSabi.Infrastructure.Persistence;

namespace WhoSabi.Infrastructure.Rooms;

public sealed class EfSubmitRoomAnswerDataStore(WhoSabiDbContext dbContext)
    : ISubmitRoomAnswerDataStore
{
    public async Task<SubmitRoomAnswerResultRecord> SubmitAnswerAsync(
        Guid userId,
        string roomId,
        string questionId,
        int selectedIndex,
        long correctAnswerRewardKobo,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var room = await dbContext.Rooms.SingleOrDefaultAsync(
            candidate => candidate.Id == roomId,
            cancellationToken);

        if (room is null)
        {
            throw new SubmitRoomAnswerException(
                "ROOM_NOT_FOUND",
                "Room was not found.");
        }

        if (room.Status != RoomStatus.Live)
        {
            throw new SubmitRoomAnswerException(
                "ROOM_NOT_LIVE",
                "Room is not accepting answers right now.");
        }

        var participant = await dbContext.RoomParticipants.SingleOrDefaultAsync(
            candidate =>
                candidate.RoomId == room.Id &&
                candidate.UserId == userId &&
                candidate.Status == RoomParticipantStatus.Active,
            cancellationToken);

        if (participant?.Role != RoomParticipantRole.Player)
        {
            throw new SubmitRoomAnswerException(
                "PLAYER_REQUIRED",
                "Only players can submit answers.");
        }

        var question = await dbContext.RoomQuestions.SingleOrDefaultAsync(
            candidate =>
                candidate.RoomId == room.Id &&
                candidate.Id == questionId,
            cancellationToken);

        if (question is null)
        {
            throw new SubmitRoomAnswerException(
                "QUESTION_NOT_FOUND",
                "Question was not found in this room.");
        }

        var optionCount = await dbContext.RoomQuestionOptions.CountAsync(
            option => option.QuestionId == question.Id,
            cancellationToken);

        if (selectedIndex >= optionCount)
        {
            throw new SubmitRoomAnswerException(
                "INVALID_SELECTED_INDEX",
                "Selected index does not exist for this question.");
        }

        var wallet = await dbContext.WalletAccounts.SingleOrDefaultAsync(
            account => account.UserId == userId,
            cancellationToken);

        if (wallet is null)
        {
            throw new SubmitRoomAnswerException(
                "WALLET_NOT_FOUND",
                "Wallet was not found.");
        }

        var existingSubmission = await dbContext.RoomAnswerSubmissions.SingleOrDefaultAsync(
            submission =>
                submission.RoomId == room.Id &&
                submission.QuestionId == question.Id &&
                submission.UserId == userId,
            cancellationToken);

        if (existingSubmission is not null)
        {
            await transaction.CommitAsync(cancellationToken);

            return new SubmitRoomAnswerResultRecord(
                existingSubmission.RoomId,
                existingSubmission.QuestionId,
                existingSubmission.SelectedIndex,
                existingSubmission.IsCorrect,
                existingSubmission.RewardKobo,
                wallet.BalanceKobo,
                existingSubmission.WalletLedgerEntryId,
                "already_submitted");
        }

        var isCorrect = question.CorrectIndex == selectedIndex;
        var rewardKobo = isCorrect ? correctAnswerRewardKobo : 0;

        var submission = new RoomAnswerSubmission
        {
            RoomId = room.Id,
            QuestionId = question.Id,
            UserId = userId,
            SelectedIndex = selectedIndex,
            IsCorrect = isCorrect,
            RewardKobo = rewardKobo,
            SubmittedAt = now
        };

        if (isCorrect)
        {
            wallet.BalanceKobo += rewardKobo;
            wallet.UpdatedAt = now;

            var ledgerEntry = new WalletLedgerEntry
            {
                WalletAccountId = wallet.Id,
                UserId = userId,
                AmountKobo = rewardKobo,
                BalanceAfterKobo = wallet.BalanceKobo,
                Type = WalletLedgerEntryType.Prize,
                Title = "Correct answer reward",
                Description = $"Correct answer reward from {room.Title}.",
                Reference = $"ANS-{submission.Id:N}",
                CreatedAt = now
            };

            submission.WalletLedgerEntryId = ledgerEntry.Id;
            dbContext.WalletLedgerEntries.Add(ledgerEntry);
        }

        dbContext.RoomAnswerSubmissions.Add(submission);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new SubmitRoomAnswerResultRecord(
            submission.RoomId,
            submission.QuestionId,
            submission.SelectedIndex,
            submission.IsCorrect,
            submission.RewardKobo,
            wallet.BalanceKobo,
            submission.WalletLedgerEntryId,
            isCorrect ? "correct_rewarded" : "incorrect");
    }
}
