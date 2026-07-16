using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WhoSabi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRooms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rooms",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Subtitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    PrizePoolKobo = table.Column<long>(type: "bigint", nullable: false),
                    PlayerCount = table.Column<int>(type: "integer", nullable: false),
                    EntryFeeKobo = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "room_gradient_colors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Hex = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_gradient_colors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_room_gradient_colors_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "room_tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Value = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_room_tags_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "rooms",
                columns: new[] { "Id", "CreatedAt", "EntryFeeKobo", "PlayerCount", "PrizePoolKobo", "Status", "Subtitle", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { "campus-clash", new DateTimeOffset(new DateTime(2026, 7, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 120000L, 712, 22500000L, "Live", "Students battle for bragging rights and a midnight payout.", "Campus Clash Arena", null },
                    { "family-fuse", new DateTimeOffset(new DateTime(2026, 7, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 65000L, 368, 9800000L, "Live", "Survey style answers with social pressure and crowd reactions.", "Family Fuse Live", null },
                    { "millionaire-rush", new DateTimeOffset(new DateTime(2026, 7, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 250000L, 932, 50000000L, "Live", "Premium stakes with dramatic countdown rounds and clutch prizes.", "Millionaire Rush", null },
                    { "naija-night", new DateTimeOffset(new DateTime(2026, 7, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 80000L, 486, 15000000L, "Live", "Fast money. Faster answers. 30 seconds to flex your knowledge.", "Naija Night Showdown", null }
                });

            migrationBuilder.InsertData(
                table: "room_gradient_colors",
                columns: new[] { "Id", "Hex", "RoomId", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("2aa2ca4f-9a13-44ce-83b5-efe5b9960001"), "#071323", "naija-night", 1 },
                    { new Guid("2aa2ca4f-9a13-44ce-83b5-efe5b9960002"), "#143D93", "naija-night", 2 },
                    { new Guid("2aa2ca4f-9a13-44ce-83b5-efe5b9960003"), "#21C9F3", "naija-night", 3 },
                    { new Guid("2aa2ca4f-9a13-44ce-83b5-efe5b9960004"), "#170A2B", "campus-clash", 1 },
                    { new Guid("2aa2ca4f-9a13-44ce-83b5-efe5b9960005"), "#5A1AB2", "campus-clash", 2 },
                    { new Guid("2aa2ca4f-9a13-44ce-83b5-efe5b9960006"), "#FF5DB1", "campus-clash", 3 },
                    { new Guid("2aa2ca4f-9a13-44ce-83b5-efe5b9960007"), "#1A1205", "family-fuse", 1 },
                    { new Guid("2aa2ca4f-9a13-44ce-83b5-efe5b9960008"), "#7E4A00", "family-fuse", 2 },
                    { new Guid("2aa2ca4f-9a13-44ce-83b5-efe5b9960009"), "#FFBE4D", "family-fuse", 3 },
                    { new Guid("2aa2ca4f-9a13-44ce-83b5-efe5b9960010"), "#060D18", "millionaire-rush", 1 },
                    { new Guid("2aa2ca4f-9a13-44ce-83b5-efe5b9960011"), "#18305F", "millionaire-rush", 2 },
                    { new Guid("2aa2ca4f-9a13-44ce-83b5-efe5b9960012"), "#4A7DFF", "millionaire-rush", 3 }
                });

            migrationBuilder.InsertData(
                table: "room_tags",
                columns: new[] { "Id", "RoomId", "SortOrder", "Value" },
                values: new object[,]
                {
                    { new Guid("65d84339-11e4-42cb-a58c-e7a1ea330001"), "naija-night", 1, "Live" },
                    { new Guid("65d84339-11e4-42cb-a58c-e7a1ea330002"), "naija-night", 2, "Trivia" },
                    { new Guid("65d84339-11e4-42cb-a58c-e7a1ea330003"), "naija-night", 3, "Hot room" },
                    { new Guid("65d84339-11e4-42cb-a58c-e7a1ea330004"), "campus-clash", 1, "Campus" },
                    { new Guid("65d84339-11e4-42cb-a58c-e7a1ea330005"), "campus-clash", 2, "Versus" },
                    { new Guid("65d84339-11e4-42cb-a58c-e7a1ea330006"), "campus-clash", 3, "Top trend" },
                    { new Guid("65d84339-11e4-42cb-a58c-e7a1ea330007"), "family-fuse", 1, "Survey" },
                    { new Guid("65d84339-11e4-42cb-a58c-e7a1ea330008"), "family-fuse", 2, "Party" },
                    { new Guid("65d84339-11e4-42cb-a58c-e7a1ea330009"), "family-fuse", 3, "Casual" },
                    { new Guid("65d84339-11e4-42cb-a58c-e7a1ea330010"), "millionaire-rush", 1, "Premium" },
                    { new Guid("65d84339-11e4-42cb-a58c-e7a1ea330011"), "millionaire-rush", 2, "Elite" },
                    { new Guid("65d84339-11e4-42cb-a58c-e7a1ea330012"), "millionaire-rush", 3, "Reward drop" }
                });

            migrationBuilder.CreateIndex(
                name: "ux_room_gradient_colors_room_id_sort_order",
                table: "room_gradient_colors",
                columns: new[] { "RoomId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_room_tags_room_id_sort_order",
                table: "room_tags",
                columns: new[] { "RoomId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rooms_status",
                table: "rooms",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "room_gradient_colors");

            migrationBuilder.DropTable(
                name: "room_tags");

            migrationBuilder.DropTable(
                name: "rooms");
        }
    }
}
