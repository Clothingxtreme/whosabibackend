using WhoSabi.Domain.Rooms;

namespace WhoSabi.Infrastructure.Persistence;

internal static class RoomContentSeedData
{
    private static readonly string[] Names =
    [
        "QuizMaster AI",
        "Ayo",
        "Mimi",
        "Tobi",
        "Kamsi",
        "Nene",
        "Zee",
        "Bolu",
        "Dami"
    ];

    private static readonly string[] Palette =
    [
        "#27D2FF",
        "#3D73FF",
        "#FF4F87",
        "#FFC24D",
        "#2247B8",
        "#1FCB73"
    ];

    public static readonly RoomDisplayParticipant[] Participants =
    [
        ..BuildParticipants("naija-night", 0),
        ..BuildParticipants("campus-clash", 11),
        ..BuildParticipants("family-fuse", 25),
        ..BuildParticipants("millionaire-rush", 40)
    ];

    public static readonly RoomQuestion[] Questions =
    [
        ..BuildQuestions("naija-night", "naija"),
        ..BuildQuestions("campus-clash", "campus"),
        ..BuildQuestions("family-fuse", "family"),
        ..BuildQuestions("millionaire-rush", "rush")
    ];

    public static readonly RoomQuestionOption[] QuestionOptions =
    [
        ..BuildQuestionOptions("naija", 1000),
        ..BuildQuestionOptions("campus", 2000),
        ..BuildQuestionOptions("family", 3000),
        ..BuildQuestionOptions("rush", 4000)
    ];

    private static RoomDisplayParticipant[] BuildParticipants(string roomId, int seed)
    {
        return Names
            .Select((name, index) => new RoomDisplayParticipant
            {
                Id = $"player-{seed}-{index}",
                RoomId = roomId,
                Name = name,
                Initials = name == "QuizMaster AI"
                    ? "AI"
                    : name[..Math.Min(2, name.Length)].ToUpperInvariant(),
                GlowColorHex = Palette[(seed + index) % Palette.Length],
                IsHostAi = index == 0,
                Score = index == 0 ? 0 : 1200 - (index * 67),
                SortOrder = index + 1
            })
            .ToArray();
    }

    private static RoomQuestion[] BuildQuestions(string roomId, string group)
    {
        return
        [
            BuildQuestion(
                $"{group}-1",
                roomId,
                "Culture",
                "Which city is widely called the Centre of Excellence in Nigeria?",
                1,
                1),
            BuildQuestion(
                $"{group}-2",
                roomId,
                "Tech",
                "What does UI stand for in product design?",
                0,
                2),
            BuildQuestion(
                $"{group}-3",
                roomId,
                "Sports",
                "How many players are on the court for one basketball team at a time?",
                1,
                3),
            BuildQuestion(
                $"{group}-4",
                roomId,
                "Business",
                "Which metric best describes total money earned before expenses?",
                0,
                4),
            BuildQuestion(
                $"{group}-5",
                roomId,
                "General",
                "Which answer is used to show uncertainty in multiple-choice banter?",
                1,
                5)
        ];
    }

    private static RoomQuestionOption[] BuildQuestionOptions(string group, int idOffset)
    {
        return
        [
            BuildOption(idOffset + 1, $"{group}-1", "Abuja", 1),
            BuildOption(idOffset + 2, $"{group}-1", "Lagos", 2),
            BuildOption(idOffset + 3, $"{group}-1", "Kano", 3),
            BuildOption(idOffset + 4, $"{group}-1", "Port Harcourt", 4),

            BuildOption(idOffset + 5, $"{group}-2", "User Interface", 1),
            BuildOption(idOffset + 6, $"{group}-2", "Universal Input", 2),
            BuildOption(idOffset + 7, $"{group}-2", "User Internet", 3),
            BuildOption(idOffset + 8, $"{group}-2", "Ultra Interaction", 4),

            BuildOption(idOffset + 9, $"{group}-3", "4", 1),
            BuildOption(idOffset + 10, $"{group}-3", "5", 2),
            BuildOption(idOffset + 11, $"{group}-3", "6", 3),
            BuildOption(idOffset + 12, $"{group}-3", "7", 4),

            BuildOption(idOffset + 13, $"{group}-4", "Revenue", 1),
            BuildOption(idOffset + 14, $"{group}-4", "Equity", 2),
            BuildOption(idOffset + 15, $"{group}-4", "Assets", 3),
            BuildOption(idOffset + 16, $"{group}-4", "Burn", 4),

            BuildOption(idOffset + 17, $"{group}-5", "Lock A", 1),
            BuildOption(idOffset + 18, $"{group}-5", "Maybe C sha", 2),
            BuildOption(idOffset + 19, $"{group}-5", "Final answer B", 3),
            BuildOption(idOffset + 20, $"{group}-5", "Skip", 4)
        ];
    }

    private static RoomQuestion BuildQuestion(
        string id,
        string roomId,
        string category,
        string prompt,
        int correctIndex,
        int sortOrder)
    {
        return new RoomQuestion
        {
            Id = id,
            RoomId = roomId,
            Category = category,
            Prompt = prompt,
            CorrectIndex = correctIndex,
            SortOrder = sortOrder
        };
    }

    private static RoomQuestionOption BuildOption(
        int id,
        string questionId,
        string value,
        int sortOrder)
    {
        return new RoomQuestionOption
        {
            Id = Guid.Parse($"10000000-0000-0000-0000-{id:D12}"),
            QuestionId = questionId,
            Value = value,
            SortOrder = sortOrder
        };
    }
}
