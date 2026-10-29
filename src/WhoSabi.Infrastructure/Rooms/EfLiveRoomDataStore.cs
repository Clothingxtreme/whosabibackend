using Microsoft.EntityFrameworkCore;
using WhoSabi.Application.Rooms;
using WhoSabi.Domain.Rooms;
using WhoSabi.Infrastructure.Persistence;

namespace WhoSabi.Infrastructure.Rooms;

public sealed class EfLiveRoomDataStore(WhoSabiDbContext dbContext) : ILiveRoomDataStore
{
    public async Task<IReadOnlyList<LiveRoomRecord>> FindLiveRoomsAsync(
        CancellationToken cancellationToken = default)
    {
        var rooms = await dbContext.Rooms
            .Where(room => room.Status == RoomStatus.Live)
            .Where(room => !room.IsTestRoom && !dbContext.Set<WhoSabi.Domain.Platform.Tournament>().Any(x => x.RoomId == room.Id && x.InviteOnly))
            .OrderByDescending(room => room.PlayerCount)
            .ThenByDescending(room => room.PrizePoolKobo)
            .ToArrayAsync(cancellationToken);

        if (rooms.Length == 0)
        {
            return [];
        }

        var roomIds = rooms.Select(room => room.Id).ToArray();

        var tags = await dbContext.RoomTags
            .Where(tag => roomIds.Contains(tag.RoomId))
            .OrderBy(tag => tag.SortOrder)
            .ToArrayAsync(cancellationToken);

        var gradientColors = await dbContext.RoomGradientColors
            .Where(color => roomIds.Contains(color.RoomId))
            .OrderBy(color => color.SortOrder)
            .ToArrayAsync(cancellationToken);

        return rooms
            .Select(room => new LiveRoomRecord(
                room.Id,
                room.Title,
                room.Subtitle,
                room.PrizePoolKobo,
                room.PlayerCount,
                room.EntryFeeKobo,
                tags
                    .Where(tag => tag.RoomId == room.Id)
                    .Select(tag => tag.Value)
                    .ToArray(),
                gradientColors
                    .Where(color => color.RoomId == room.Id)
                    .Select(color => color.Hex)
                    .ToArray()))
            .ToArray();
    }
}
