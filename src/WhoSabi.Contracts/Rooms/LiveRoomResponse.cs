namespace WhoSabi.Contracts.Rooms;

public sealed record LiveRoomResponse(
    string Id,
    string Title,
    string Subtitle,
    long PrizePoolKobo,
    decimal PrizePool,
    int PlayerCount,
    long EntryFeeKobo,
    decimal EntryFee,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> GradientColors);
