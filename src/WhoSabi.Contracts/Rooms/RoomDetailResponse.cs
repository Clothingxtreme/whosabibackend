namespace WhoSabi.Contracts.Rooms;

public sealed record RoomDetailResponse(
    string Id,
    string Title,
    string Subtitle,
    long PrizePoolKobo,
    decimal PrizePool,
    int PlayerCount,
    long EntryFeeKobo,
    decimal EntryFee,
    string ViewerRole,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> GradientColors,
    IReadOnlyList<RoomParticipantResponse> Participants,
    IReadOnlyList<RoomQuestionResponse> Questions);
