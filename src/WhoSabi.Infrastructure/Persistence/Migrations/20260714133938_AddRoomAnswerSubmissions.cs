using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhoSabi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomAnswerSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "room_answer_submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    QuestionId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectedIndex = table.Column<int>(type: "integer", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    RewardKobo = table.Column<long>(type: "bigint", nullable: false),
                    WalletLedgerEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_answer_submissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_room_answer_submissions_room_questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "room_questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_room_answer_submissions_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_room_answer_submissions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_room_answer_submissions_wallet_ledger_entries_WalletLedgerE~",
                        column: x => x.WalletLedgerEntryId,
                        principalTable: "wallet_ledger_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_room_answer_submissions_QuestionId",
                table: "room_answer_submissions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "ix_room_answer_submissions_user_id",
                table: "room_answer_submissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_room_answer_submissions_wallet_ledger_entry_id",
                table: "room_answer_submissions",
                column: "WalletLedgerEntryId");

            migrationBuilder.CreateIndex(
                name: "ux_room_answer_submissions_room_question_user",
                table: "room_answer_submissions",
                columns: new[] { "RoomId", "QuestionId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "room_answer_submissions");
        }
    }
}
