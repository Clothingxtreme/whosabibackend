namespace WhoSabi.Contracts.Rooms;

public sealed record JoinRoomResponse(
    string RoomId,
    Guid UserId,
    string Role,
    long EntryFeeKobo,
    decimal EntryFee,
    long WalletBalanceKobo,
    decimal WalletBalance,
    Guid? WalletTransactionId,
    string Status);
