using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhoSabi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackendIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep login identifiers unique without silently renaming existing users.
            migrationBuilder.Sql("CREATE UNIQUE INDEX ux_users_username_lower ON users (lower(\"Username\"));");
            migrationBuilder.AddColumn<string>(
                name: "AcceptedPolicyVersion",
                table: "users",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "users",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleSubject",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTestAccount",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "users",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PolicyAcceptedAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HostUserId",
                table: "rooms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTestRoom",
                table: "rooms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanPublishMedia",
                table: "room_participants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsBanned",
                table: "room_participants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AcceptedPolicyVersion",
                table: "pending_signups",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "pending_signups",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PolicyAcceptedAt",
                table: "pending_signups",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "payment_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "bank_accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    AccountNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    AccountName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bank_accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bank_accounts_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<string>(type: "character varying(80)", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientMessageId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_messages_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chat_messages_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Body = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Link = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notifications_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "push_devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    Platform = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_push_devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_push_devices_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "room_rounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<string>(type: "character varying(80)", nullable: false),
                    QuestionId = table.Column<string>(type: "character varying(80)", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_rounds", x => x.Id);
                    table.CheckConstraint("ck_round_deadline", "\"EndsAt\" >= \"StartedAt\"");
                    table.ForeignKey(
                        name: "FK_room_rounds_room_questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "room_questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_room_rounds_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tournaments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<string>(type: "character varying(80)", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    InviteOnly = table.Column<bool>(type: "boolean", nullable: false),
                    EntryFeeKobo = table.Column<long>(type: "bigint", nullable: false),
                    PoolKobo = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SettledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournaments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tournaments_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tournaments_users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "wallet_operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientId = table.Column<Guid>(type: "uuid", nullable: true),
                    Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Fingerprint = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AmountKobo = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallet_operations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wallet_operations_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "push_deliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_push_deliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_push_deliveries_notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_push_deliveries_push_devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "push_devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tournament_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaidKobo = table.Column<long>(type: "bigint", nullable: false),
                    PayoutKobo = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tournament_entries_tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tournament_entries_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tournament_invites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_invites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tournament_invites_tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tournament_invites_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Remove invented sample metrics while preserving actual room activity.
            migrationBuilder.Sql("""
                UPDATE rooms r
                SET "PlayerCount" = (SELECT count(*) FROM room_participants p WHERE p."RoomId" = r."Id" AND p."Role" = 'Player' AND p."Status" = 'Active'),
                    "PrizePoolKobo" = 0,
                    "Status" = CASE WHEN EXISTS (SELECT 1 FROM room_participants p WHERE p."RoomId" = r."Id") THEN r."Status" ELSE 'Scheduled' END
                WHERE r."Id" IN ('campus-clash', 'family-fuse', 'millionaire-rush', 'naija-night');
                """);
            migrationBuilder.CreateIndex(
                name: "IX_wallet_ledger_entries_WalletAccountId_Reference",
                table: "wallet_ledger_entries",
                columns: new[] { "WalletAccountId", "Reference" },
                unique: true,
                filter: "\"Reference\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_wallet_balance_nonnegative",
                table: "wallet_accounts",
                sql: "\"BalanceKobo\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_users_GoogleSubject",
                table: "users",
                column: "GoogleSubject",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_PhoneNumber",
                table: "users",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rooms_HostUserId",
                table: "rooms",
                column: "HostUserId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_UserId",
                table: "payment_transactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_bank_accounts_UserId_BankCode_AccountNumber",
                table: "bank_accounts",
                columns: new[] { "UserId", "BankCode", "AccountNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_RoomId_CreatedAt",
                table: "chat_messages",
                columns: new[] { "RoomId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_RoomId_UserId_ClientMessageId",
                table: "chat_messages",
                columns: new[] { "RoomId", "UserId", "ClientMessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_UserId",
                table: "chat_messages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId_CreatedAt",
                table: "notifications",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_push_deliveries_DeviceId",
                table: "push_deliveries",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_push_deliveries_NotificationId_DeviceId",
                table: "push_deliveries",
                columns: new[] { "NotificationId", "DeviceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_push_devices_Token",
                table: "push_devices",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_push_devices_UserId",
                table: "push_devices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_room_rounds_QuestionId",
                table: "room_rounds",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_room_rounds_RoomId",
                table: "room_rounds",
                column: "RoomId",
                unique: true,
                filter: "\"ClosedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_room_rounds_RoomId_QuestionId",
                table: "room_rounds",
                columns: new[] { "RoomId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tournament_entries_TournamentId_UserId",
                table: "tournament_entries",
                columns: new[] { "TournamentId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tournament_entries_UserId",
                table: "tournament_entries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_invites_TournamentId_UserId",
                table: "tournament_invites",
                columns: new[] { "TournamentId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tournament_invites_UserId",
                table: "tournament_invites",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tournaments_OwnerId",
                table: "tournaments",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_tournaments_RoomId",
                table: "tournaments",
                column: "RoomId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wallet_operations_UserId_Key",
                table: "wallet_operations",
                columns: new[] { "UserId", "Key" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_payment_transactions_users_UserId",
                table: "payment_transactions",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_rooms_users_HostUserId",
                table: "rooms",
                column: "HostUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX ux_users_username_lower;");
            migrationBuilder.DropForeignKey(
                name: "FK_payment_transactions_users_UserId",
                table: "payment_transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_rooms_users_HostUserId",
                table: "rooms");

            migrationBuilder.DropTable(
                name: "bank_accounts");

            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "push_deliveries");

            migrationBuilder.DropTable(
                name: "room_rounds");

            migrationBuilder.DropTable(
                name: "tournament_entries");

            migrationBuilder.DropTable(
                name: "tournament_invites");

            migrationBuilder.DropTable(
                name: "wallet_operations");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "push_devices");

            migrationBuilder.DropTable(
                name: "tournaments");

            migrationBuilder.DropIndex(
                name: "IX_wallet_ledger_entries_WalletAccountId_Reference",
                table: "wallet_ledger_entries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_wallet_balance_nonnegative",
                table: "wallet_accounts");

            migrationBuilder.DropIndex(
                name: "IX_users_GoogleSubject",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_PhoneNumber",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_rooms_HostUserId",
                table: "rooms");

            migrationBuilder.DropIndex(
                name: "IX_payment_transactions_UserId",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "AcceptedPolicyVersion",
                table: "users");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "users");

            migrationBuilder.DropColumn(
                name: "GoogleSubject",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsTestAccount",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PolicyAcceptedAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "HostUserId",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "IsTestRoom",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "CanPublishMedia",
                table: "room_participants");

            migrationBuilder.DropColumn(
                name: "IsBanned",
                table: "room_participants");

            migrationBuilder.DropColumn(
                name: "AcceptedPolicyVersion",
                table: "pending_signups");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "pending_signups");

            migrationBuilder.DropColumn(
                name: "PolicyAcceptedAt",
                table: "pending_signups");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "payment_transactions");

            migrationBuilder.UpdateData(
                table: "rooms",
                keyColumn: "Id",
                keyValue: "campus-clash",
                columns: new[] { "PlayerCount", "PrizePoolKobo", "Status" },
                values: new object[] { 712, 22500000L, "Live" });

            migrationBuilder.UpdateData(
                table: "rooms",
                keyColumn: "Id",
                keyValue: "family-fuse",
                columns: new[] { "PlayerCount", "PrizePoolKobo", "Status" },
                values: new object[] { 368, 9800000L, "Live" });

            migrationBuilder.UpdateData(
                table: "rooms",
                keyColumn: "Id",
                keyValue: "millionaire-rush",
                columns: new[] { "PlayerCount", "PrizePoolKobo", "Status" },
                values: new object[] { 932, 50000000L, "Live" });

            migrationBuilder.UpdateData(
                table: "rooms",
                keyColumn: "Id",
                keyValue: "naija-night",
                columns: new[] { "PlayerCount", "PrizePoolKobo", "Status" },
                values: new object[] { 486, 15000000L, "Live" });
        }
    }
}
