namespace WhoSabi.Application.Rooms;

public sealed record RoomDetailRecord(
    string Id,
    string Title,
    string Subtitle,
    long PrizePoolKobo,
    int PlayerCount,
    long EntryFeeKobo,
    string ViewerRole,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> GradientColors,
    IReadOnlyList<RoomDetailParticipantRecord> Participants,
    IReadOnlyList<RoomDetailQuestionRecord> Questions);

public sealed record RoomDetailParticipantRecord(
    string Id,
    string Name,
    string Initials,
    string GlowColorHex,
    bool IsHostAi,
    int Score);

public sealed record RoomDetailQuestionRecord(
    string Id,
    string Category,
    string Prompt,
    IReadOnlyList<string> Options);
