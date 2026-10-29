using Microsoft.EntityFrameworkCore;
using WhoSabi.Application.Rooms;
using WhoSabi.Domain.Rooms;
using WhoSabi.Infrastructure.Persistence;

namespace WhoSabi.Infrastructure.Rooms;

public sealed class EfRoomDetailDataStore(WhoSabiDbContext dbContext) : IRoomDetailDataStore
{
    public async Task<RoomDetailRecord?> FindRoomDetailAsync(
        Guid userId,
        string roomId,
        CancellationToken cancellationToken = default)
    {
        var room = await dbContext.Rooms.SingleOrDefaultAsync(
            candidate => candidate.Id == roomId,
            cancellationToken);

        if (room is null)
        {
            return null;
        }
        var tournament = await dbContext.Set<WhoSabi.Domain.Platform.Tournament>().SingleOrDefaultAsync(x => x.RoomId == roomId, cancellationToken);
        if (tournament is { InviteOnly: true } && tournament.OwnerId != userId &&
            !await dbContext.Set<WhoSabi.Domain.Platform.TournamentInvite>().AnyAsync(x => x.TournamentId == tournament.Id && x.UserId == userId && x.Status == "accepted", cancellationToken))
            return null;
        if (room.IsTestRoom && !await dbContext.Users.AnyAsync(x => x.Id == userId && x.IsTestAccount, cancellationToken)) return null;

        var participant = await dbContext.RoomParticipants.SingleOrDefaultAsync(
            candidate =>
                candidate.RoomId == room.Id &&
                candidate.UserId == userId &&
                candidate.Status == RoomParticipantStatus.Active,
            cancellationToken);

        var tags = await dbContext.RoomTags
            .Where(tag => tag.RoomId == room.Id)
            .OrderBy(tag => tag.SortOrder)
            .Select(tag => tag.Value)
            .ToArrayAsync(cancellationToken);

        var gradientColors = await dbContext.RoomGradientColors
            .Where(color => color.RoomId == room.Id)
            .OrderBy(color => color.SortOrder)
            .Select(color => color.Hex)
            .ToArrayAsync(cancellationToken);

        var participants = await dbContext.RoomParticipants
            .Where(p => p.RoomId == room.Id && p.Status == RoomParticipantStatus.Active)
            .Join(dbContext.Users, p => p.UserId, u => u.Id, (p, u) => new RoomDetailParticipantRecord(
                u.Id.ToString(), u.Username, u.AvatarLabel, "#21C9F3", false,
                dbContext.RoomAnswerSubmissions.Count(s => s.RoomId == roomId && s.UserId == u.Id && s.IsCorrect && dbContext.Set<WhoSabi.Domain.Platform.RoomRound>().Any(r => r.QuestionId == s.QuestionId && r.ClosedAt != null))))
            .ToArrayAsync(cancellationToken);

        var questions = await dbContext.RoomQuestions
            .Where(question => question.RoomId == room.Id)
            .Where(question => dbContext.Set<WhoSabi.Domain.Platform.RoomRound>().Any(r => r.RoomId == room.Id && r.QuestionId == question.Id))
            .OrderBy(question => question.SortOrder)
            .Select(question => new
            {
                question.Id,
                question.Category,
                question.Prompt
            })
            .ToArrayAsync(cancellationToken);

        var questionIds = questions.Select(question => question.Id).ToArray();
        var options = await dbContext.RoomQuestionOptions
            .Where(option => questionIds.Contains(option.QuestionId))
            .OrderBy(option => option.SortOrder)
            .ToArrayAsync(cancellationToken);

        return new RoomDetailRecord(
            room.Id,
            room.Title,
            room.Subtitle,
            room.PrizePoolKobo,
            room.PlayerCount,
            room.EntryFeeKobo,
            participant?.Role.ToString().ToLowerInvariant() ?? "spectator",
            tags,
            gradientColors,
            participants,
            questions.Select(question => new RoomDetailQuestionRecord(
                question.Id,
                question.Category,
                question.Prompt,
                options
                    .Where(option => option.QuestionId == question.Id)
                    .OrderBy(option => option.SortOrder)
                    .Select(option => option.Value)
                    .ToArray())).ToArray());
    }
}
