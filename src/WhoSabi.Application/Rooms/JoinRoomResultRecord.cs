using WhoSabi.Domain.Rooms;

namespace WhoSabi.Application.Rooms;

public sealed record JoinRoomResultRecord(
    string RoomId,
    Guid UserId,
    RoomParticipantRole Role,
    long EntryFeeKobo,
    long WalletBalanceKobo,
    Guid? WalletTransactionId,
    string Status);
