using WhoSabi.Contracts.Rooms;

namespace WhoSabi.Application.Rooms;

public sealed class GetLiveRoomsUseCase(ILiveRoomDataStore dataStore)
    : IGetLiveRoomsUseCase
{
    public async Task<IReadOnlyList<LiveRoomResponse>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var rooms = await dataStore.FindLiveRoomsAsync(cancellationToken);

        return rooms
            .Select(room => new LiveRoomResponse(
                room.Id,
                room.Title,
                room.Subtitle,
                room.PrizePoolKobo,
                room.PrizePoolKobo / 100m,
                room.PlayerCount,
                room.EntryFeeKobo,
                room.EntryFeeKobo / 100m,
                room.Tags,
                room.GradientColors))
            .ToArray();
    }
}
