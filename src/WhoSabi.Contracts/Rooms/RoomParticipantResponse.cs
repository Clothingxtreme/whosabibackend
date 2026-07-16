namespace WhoSabi.Contracts.Rooms;

public sealed record RoomParticipantResponse(
    string Id,
    string Name,
    string Initials,
    string GlowColorHex,
    bool IsHostAi,
    int Score);
