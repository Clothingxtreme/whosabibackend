using WhoSabi.Contracts.Rooms;
using WhoSabi.Domain.Rooms;

namespace WhoSabi.Application.Rooms;

public sealed class JoinRoomUseCase(IJoinRoomDataStore dataStore) : IJoinRoomUseCase
{
    public async Task<JoinRoomResponse> HandleAsync(
        Guid userId,
        string roomId,
        JoinRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new JoinRoomException(
                "INVALID_USER",
                "A valid authenticated user is required.");
        }

        var normalizedRoomId = roomId.Trim();
        if (string.IsNullOrWhiteSpace(normalizedRoomId) || normalizedRoomId.Length > 80)
        {
            throw new JoinRoomException(
                "INVALID_ROOM_ID",
                "A valid room id is required.");
        }

        var role = NormalizeRole(request.Mode);
        var now = DateTimeOffset.UtcNow;

        var result = await dataStore.JoinRoomAsync(
            userId,
            normalizedRoomId,
            role,
            now,
            cancellationToken);

        return new JoinRoomResponse(
            result.RoomId,
            result.UserId,
            result.Role.ToString().ToLowerInvariant(),
            result.EntryFeeKobo,
            result.EntryFeeKobo / 100m,
            result.WalletBalanceKobo,
            result.WalletBalanceKobo / 100m,
            result.WalletTransactionId,
            result.Status);
    }

    private static RoomParticipantRole NormalizeRole(string? mode)
    {
        var normalized = mode?.Trim().ToLowerInvariant();

        return normalized switch
        {
            "player" => RoomParticipantRole.Player,
            "spectator" => RoomParticipantRole.Spectator,
            _ => throw new JoinRoomException(
                "INVALID_JOIN_MODE",
                "Join mode must be either 'player' or 'spectator'.")
        };
    }
}
