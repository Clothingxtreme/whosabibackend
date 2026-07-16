using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WhoSabi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "room_display_participants",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RoomId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Initials = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    GlowColorHex = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsHostAi = table.Column<bool>(type: "boolean", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_display_participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_room_display_participants_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "room_questions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RoomId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Prompt = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CorrectIndex = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_room_questions_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "room_question_options",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_question_options", x => x.Id);
                    table.ForeignKey(
                        name: "FK_room_question_options_room_questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "room_questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "room_display_participants",
                columns: new[] { "Id", "GlowColorHex", "Initials", "IsHostAi", "Name", "RoomId", "Score", "SortOrder" },
                values: new object[,]
                {
                    { "player-0-0", "#27D2FF", "AI", true, "QuizMaster AI", "naija-night", 0, 1 },
                    { "player-0-1", "#3D73FF", "AY", false, "Ayo", "naija-night", 1133, 2 },
                    { "player-0-2", "#FF4F87", "MI", false, "Mimi", "naija-night", 1066, 3 },
                    { "player-0-3", "#FFC24D", "TO", false, "Tobi", "naija-night", 999, 4 },
                    { "player-0-4", "#2247B8", "KA", false, "Kamsi", "naija-night", 932, 5 },
                    { "player-0-5", "#1FCB73", "NE", false, "Nene", "naija-night", 865, 6 },
                    { "player-0-6", "#27D2FF", "ZE", false, "Zee", "naija-night", 798, 7 },
                    { "player-0-7", "#3D73FF", "BO", false, "Bolu", "naija-night", 731, 8 },
                    { "player-0-8", "#FF4F87", "DA", false, "Dami", "naija-night", 664, 9 },
                    { "player-11-0", "#1FCB73", "AI", true, "QuizMaster AI", "campus-clash", 0, 1 },
                    { "player-11-1", "#27D2FF", "AY", false, "Ayo", "campus-clash", 1133, 2 },
                    { "player-11-2", "#3D73FF", "MI", false, "Mimi", "campus-clash", 1066, 3 },
                    { "player-11-3", "#FF4F87", "TO", false, "Tobi", "campus-clash", 999, 4 },
                    { "player-11-4", "#FFC24D", "KA", false, "Kamsi", "campus-clash", 932, 5 },
                    { "player-11-5", "#2247B8", "NE", false, "Nene", "campus-clash", 865, 6 },
                    { "player-11-6", "#1FCB73", "ZE", false, "Zee", "campus-clash", 798, 7 },
                    { "player-11-7", "#27D2FF", "BO", false, "Bolu", "campus-clash", 731, 8 },
                    { "player-11-8", "#3D73FF", "DA", false, "Dami", "campus-clash", 664, 9 },
                    { "player-25-0", "#3D73FF", "AI", true, "QuizMaster AI", "family-fuse", 0, 1 },
                    { "player-25-1", "#FF4F87", "AY", false, "Ayo", "family-fuse", 1133, 2 },
                    { "player-25-2", "#FFC24D", "MI", false, "Mimi", "family-fuse", 1066, 3 },
                    { "player-25-3", "#2247B8", "TO", false, "Tobi", "family-fuse", 999, 4 },
                    { "player-25-4", "#1FCB73", "KA", false, "Kamsi", "family-fuse", 932, 5 },
                    { "player-25-5", "#27D2FF", "NE", false, "Nene", "family-fuse", 865, 6 },
                    { "player-25-6", "#3D73FF", "ZE", false, "Zee", "family-fuse", 798, 7 },
                    { "player-25-7", "#FF4F87", "BO", false, "Bolu", "family-fuse", 731, 8 },
                    { "player-25-8", "#FFC24D", "DA", false, "Dami", "family-fuse", 664, 9 },
                    { "player-40-0", "#2247B8", "AI", true, "QuizMaster AI", "millionaire-rush", 0, 1 },
                    { "player-40-1", "#1FCB73", "AY", false, "Ayo", "millionaire-rush", 1133, 2 },
                    { "player-40-2", "#27D2FF", "MI", false, "Mimi", "millionaire-rush", 1066, 3 },
                    { "player-40-3", "#3D73FF", "TO", false, "Tobi", "millionaire-rush", 999, 4 },
                    { "player-40-4", "#FF4F87", "KA", false, "Kamsi", "millionaire-rush", 932, 5 },
                    { "player-40-5", "#FFC24D", "NE", false, "Nene", "millionaire-rush", 865, 6 },
                    { "player-40-6", "#2247B8", "ZE", false, "Zee", "millionaire-rush", 798, 7 },
                    { "player-40-7", "#1FCB73", "BO", false, "Bolu", "millionaire-rush", 731, 8 },
                    { "player-40-8", "#27D2FF", "DA", false, "Dami", "millionaire-rush", 664, 9 }
                });

            migrationBuilder.InsertData(
                table: "room_questions",
                columns: new[] { "Id", "Category", "CorrectIndex", "Prompt", "RoomId", "SortOrder" },
                values: new object[,]
                {
                    { "campus-1", "Culture", 1, "Which city is widely called the Centre of Excellence in Nigeria?", "campus-clash", 1 },
                    { "campus-2", "Tech", 0, "What does UI stand for in product design?", "campus-clash", 2 },
                    { "campus-3", "Sports", 1, "How many players are on the court for one basketball team at a time?", "campus-clash", 3 },
                    { "campus-4", "Business", 0, "Which metric best describes total money earned before expenses?", "campus-clash", 4 },
                    { "campus-5", "General", 1, "Which answer is used to show uncertainty in multiple-choice banter?", "campus-clash", 5 },
                    { "family-1", "Culture", 1, "Which city is widely called the Centre of Excellence in Nigeria?", "family-fuse", 1 },
                    { "family-2", "Tech", 0, "What does UI stand for in product design?", "family-fuse", 2 },
                    { "family-3", "Sports", 1, "How many players are on the court for one basketball team at a time?", "family-fuse", 3 },
                    { "family-4", "Business", 0, "Which metric best describes total money earned before expenses?", "family-fuse", 4 },
                    { "family-5", "General", 1, "Which answer is used to show uncertainty in multiple-choice banter?", "family-fuse", 5 },
                    { "naija-1", "Culture", 1, "Which city is widely called the Centre of Excellence in Nigeria?", "naija-night", 1 },
                    { "naija-2", "Tech", 0, "What does UI stand for in product design?", "naija-night", 2 },
                    { "naija-3", "Sports", 1, "How many players are on the court for one basketball team at a time?", "naija-night", 3 },
                    { "naija-4", "Business", 0, "Which metric best describes total money earned before expenses?", "naija-night", 4 },
                    { "naija-5", "General", 1, "Which answer is used to show uncertainty in multiple-choice banter?", "naija-night", 5 },
                    { "rush-1", "Culture", 1, "Which city is widely called the Centre of Excellence in Nigeria?", "millionaire-rush", 1 },
                    { "rush-2", "Tech", 0, "What does UI stand for in product design?", "millionaire-rush", 2 },
                    { "rush-3", "Sports", 1, "How many players are on the court for one basketball team at a time?", "millionaire-rush", 3 },
                    { "rush-4", "Business", 0, "Which metric best describes total money earned before expenses?", "millionaire-rush", 4 },
                    { "rush-5", "General", 1, "Which answer is used to show uncertainty in multiple-choice banter?", "millionaire-rush", 5 }
                });

            migrationBuilder.InsertData(
                table: "room_question_options",
                columns: new[] { "Id", "QuestionId", "SortOrder", "Value" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000001001"), "naija-1", 1, "Abuja" },
                    { new Guid("10000000-0000-0000-0000-000000001002"), "naija-1", 2, "Lagos" },
                    { new Guid("10000000-0000-0000-0000-000000001003"), "naija-1", 3, "Kano" },
                    { new Guid("10000000-0000-0000-0000-000000001004"), "naija-1", 4, "Port Harcourt" },
                    { new Guid("10000000-0000-0000-0000-000000001005"), "naija-2", 1, "User Interface" },
                    { new Guid("10000000-0000-0000-0000-000000001006"), "naija-2", 2, "Universal Input" },
                    { new Guid("10000000-0000-0000-0000-000000001007"), "naija-2", 3, "User Internet" },
                    { new Guid("10000000-0000-0000-0000-000000001008"), "naija-2", 4, "Ultra Interaction" },
                    { new Guid("10000000-0000-0000-0000-000000001009"), "naija-3", 1, "4" },
                    { new Guid("10000000-0000-0000-0000-000000001010"), "naija-3", 2, "5" },
                    { new Guid("10000000-0000-0000-0000-000000001011"), "naija-3", 3, "6" },
                    { new Guid("10000000-0000-0000-0000-000000001012"), "naija-3", 4, "7" },
                    { new Guid("10000000-0000-0000-0000-000000001013"), "naija-4", 1, "Revenue" },
                    { new Guid("10000000-0000-0000-0000-000000001014"), "naija-4", 2, "Equity" },
                    { new Guid("10000000-0000-0000-0000-000000001015"), "naija-4", 3, "Assets" },
                    { new Guid("10000000-0000-0000-0000-000000001016"), "naija-4", 4, "Burn" },
                    { new Guid("10000000-0000-0000-0000-000000001017"), "naija-5", 1, "Lock A" },
                    { new Guid("10000000-0000-0000-0000-000000001018"), "naija-5", 2, "Maybe C sha" },
                    { new Guid("10000000-0000-0000-0000-000000001019"), "naija-5", 3, "Final answer B" },
                    { new Guid("10000000-0000-0000-0000-000000001020"), "naija-5", 4, "Skip" },
                    { new Guid("10000000-0000-0000-0000-000000002001"), "campus-1", 1, "Abuja" },
                    { new Guid("10000000-0000-0000-0000-000000002002"), "campus-1", 2, "Lagos" },
                    { new Guid("10000000-0000-0000-0000-000000002003"), "campus-1", 3, "Kano" },
                    { new Guid("10000000-0000-0000-0000-000000002004"), "campus-1", 4, "Port Harcourt" },
                    { new Guid("10000000-0000-0000-0000-000000002005"), "campus-2", 1, "User Interface" },
                    { new Guid("10000000-0000-0000-0000-000000002006"), "campus-2", 2, "Universal Input" },
                    { new Guid("10000000-0000-0000-0000-000000002007"), "campus-2", 3, "User Internet" },
                    { new Guid("10000000-0000-0000-0000-000000002008"), "campus-2", 4, "Ultra Interaction" },
                    { new Guid("10000000-0000-0000-0000-000000002009"), "campus-3", 1, "4" },
                    { new Guid("10000000-0000-0000-0000-000000002010"), "campus-3", 2, "5" },
                    { new Guid("10000000-0000-0000-0000-000000002011"), "campus-3", 3, "6" },
                    { new Guid("10000000-0000-0000-0000-000000002012"), "campus-3", 4, "7" },
                    { new Guid("10000000-0000-0000-0000-000000002013"), "campus-4", 1, "Revenue" },
                    { new Guid("10000000-0000-0000-0000-000000002014"), "campus-4", 2, "Equity" },
                    { new Guid("10000000-0000-0000-0000-000000002015"), "campus-4", 3, "Assets" },
                    { new Guid("10000000-0000-0000-0000-000000002016"), "campus-4", 4, "Burn" },
                    { new Guid("10000000-0000-0000-0000-000000002017"), "campus-5", 1, "Lock A" },
                    { new Guid("10000000-0000-0000-0000-000000002018"), "campus-5", 2, "Maybe C sha" },
                    { new Guid("10000000-0000-0000-0000-000000002019"), "campus-5", 3, "Final answer B" },
                    { new Guid("10000000-0000-0000-0000-000000002020"), "campus-5", 4, "Skip" },
                    { new Guid("10000000-0000-0000-0000-000000003001"), "family-1", 1, "Abuja" },
                    { new Guid("10000000-0000-0000-0000-000000003002"), "family-1", 2, "Lagos" },
                    { new Guid("10000000-0000-0000-0000-000000003003"), "family-1", 3, "Kano" },
                    { new Guid("10000000-0000-0000-0000-000000003004"), "family-1", 4, "Port Harcourt" },
                    { new Guid("10000000-0000-0000-0000-000000003005"), "family-2", 1, "User Interface" },
                    { new Guid("10000000-0000-0000-0000-000000003006"), "family-2", 2, "Universal Input" },
                    { new Guid("10000000-0000-0000-0000-000000003007"), "family-2", 3, "User Internet" },
                    { new Guid("10000000-0000-0000-0000-000000003008"), "family-2", 4, "Ultra Interaction" },
                    { new Guid("10000000-0000-0000-0000-000000003009"), "family-3", 1, "4" },
                    { new Guid("10000000-0000-0000-0000-000000003010"), "family-3", 2, "5" },
                    { new Guid("10000000-0000-0000-0000-000000003011"), "family-3", 3, "6" },
                    { new Guid("10000000-0000-0000-0000-000000003012"), "family-3", 4, "7" },
                    { new Guid("10000000-0000-0000-0000-000000003013"), "family-4", 1, "Revenue" },
                    { new Guid("10000000-0000-0000-0000-000000003014"), "family-4", 2, "Equity" },
                    { new Guid("10000000-0000-0000-0000-000000003015"), "family-4", 3, "Assets" },
                    { new Guid("10000000-0000-0000-0000-000000003016"), "family-4", 4, "Burn" },
                    { new Guid("10000000-0000-0000-0000-000000003017"), "family-5", 1, "Lock A" },
                    { new Guid("10000000-0000-0000-0000-000000003018"), "family-5", 2, "Maybe C sha" },
                    { new Guid("10000000-0000-0000-0000-000000003019"), "family-5", 3, "Final answer B" },
                    { new Guid("10000000-0000-0000-0000-000000003020"), "family-5", 4, "Skip" },
                    { new Guid("10000000-0000-0000-0000-000000004001"), "rush-1", 1, "Abuja" },
                    { new Guid("10000000-0000-0000-0000-000000004002"), "rush-1", 2, "Lagos" },
                    { new Guid("10000000-0000-0000-0000-000000004003"), "rush-1", 3, "Kano" },
                    { new Guid("10000000-0000-0000-0000-000000004004"), "rush-1", 4, "Port Harcourt" },
                    { new Guid("10000000-0000-0000-0000-000000004005"), "rush-2", 1, "User Interface" },
                    { new Guid("10000000-0000-0000-0000-000000004006"), "rush-2", 2, "Universal Input" },
                    { new Guid("10000000-0000-0000-0000-000000004007"), "rush-2", 3, "User Internet" },
                    { new Guid("10000000-0000-0000-0000-000000004008"), "rush-2", 4, "Ultra Interaction" },
                    { new Guid("10000000-0000-0000-0000-000000004009"), "rush-3", 1, "4" },
                    { new Guid("10000000-0000-0000-0000-000000004010"), "rush-3", 2, "5" },
                    { new Guid("10000000-0000-0000-0000-000000004011"), "rush-3", 3, "6" },
                    { new Guid("10000000-0000-0000-0000-000000004012"), "rush-3", 4, "7" },
                    { new Guid("10000000-0000-0000-0000-000000004013"), "rush-4", 1, "Revenue" },
                    { new Guid("10000000-0000-0000-0000-000000004014"), "rush-4", 2, "Equity" },
                    { new Guid("10000000-0000-0000-0000-000000004015"), "rush-4", 3, "Assets" },
                    { new Guid("10000000-0000-0000-0000-000000004016"), "rush-4", 4, "Burn" },
                    { new Guid("10000000-0000-0000-0000-000000004017"), "rush-5", 1, "Lock A" },
                    { new Guid("10000000-0000-0000-0000-000000004018"), "rush-5", 2, "Maybe C sha" },
                    { new Guid("10000000-0000-0000-0000-000000004019"), "rush-5", 3, "Final answer B" },
                    { new Guid("10000000-0000-0000-0000-000000004020"), "rush-5", 4, "Skip" }
                });

            migrationBuilder.CreateIndex(
                name: "ux_room_display_participants_room_id_sort_order",
                table: "room_display_participants",
                columns: new[] { "RoomId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_room_question_options_question_id_sort_order",
                table: "room_question_options",
                columns: new[] { "QuestionId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_room_questions_room_id_sort_order",
                table: "room_questions",
                columns: new[] { "RoomId", "SortOrder" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "room_display_participants");

            migrationBuilder.DropTable(
                name: "room_question_options");

            migrationBuilder.DropTable(
                name: "room_questions");
        }
    }
}
