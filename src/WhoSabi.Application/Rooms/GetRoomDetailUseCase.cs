using WhoSabi.Contracts.Rooms;

namespace WhoSabi.Application.Rooms;

public sealed class GetRoomDetailUseCase(IRoomDetailDataStore dataStore)
    : IGetRoomDetailUseCase
{
    public async Task<RoomDetailResponse> HandleAsync(
        Guid userId,
        string roomId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new RoomDetailException(
                "INVALID_USER",
                "A valid authenticated user is required.");
        }

        var normalizedRoomId = roomId.Trim();
        if (string.IsNullOrWhiteSpace(normalizedRoomId) || normalizedRoomId.Length > 80)
        {
            throw new RoomDetailException(
                "INVALID_ROOM_ID",
                "A valid room id is required.");
        }

        var room = await dataStore.FindRoomDetailAsync(
            userId,
            normalizedRoomId,
            cancellationToken);

        if (room is null)
        {
            throw new RoomDetailException(
                "ROOM_NOT_FOUND",
                "Room was not found.");
        }

        return new RoomDetailResponse(
            room.Id,
            room.Title,
            room.Subtitle,
            room.PrizePoolKobo,
            room.PrizePoolKobo / 100m,
            room.PlayerCount,
            room.EntryFeeKobo,
            room.EntryFeeKobo / 100m,
            room.ViewerRole,
            room.Tags,
            room.GradientColors,
            room.Participants.Select(participant => new RoomParticipantResponse(
                participant.Id,
                participant.Name,
                participant.Initials,
                participant.GlowColorHex,
                participant.IsHostAi,
                participant.Score)).ToArray(),
            room.Questions.Select(question => new RoomQuestionResponse(
                question.Id,
                question.Category,
                question.Prompt,
                question.Options)).ToArray());
    }
}
