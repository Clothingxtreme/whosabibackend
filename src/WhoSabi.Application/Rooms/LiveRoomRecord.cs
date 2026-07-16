namespace WhoSabi.Application.Rooms;

public sealed record LiveRoomRecord(
    string Id,
    string Title,
    string Subtitle,
    long PrizePoolKobo,
    int PlayerCount,
    long EntryFeeKobo,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> GradientColors);
