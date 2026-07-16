using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhoSabi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersAndWallets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AvatarLabel = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    ReferralCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    HasPaidRegistrationFee = table.Column<bool>(type: "boolean", nullable: false),
                    GamesPlayed = table.Column<int>(type: "integer", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "wallet_accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BalanceKobo = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallet_accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wallet_accounts_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "wallet_ledger_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AmountKobo = table.Column<long>(type: "bigint", nullable: false),
                    BalanceAfterKobo = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Reference = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallet_ledger_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wallet_ledger_entries_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wallet_ledger_entries_wallet_accounts_WalletAccountId",
                        column: x => x.WalletAccountId,
                        principalTable: "wallet_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_users_normalized_email",
                table: "users",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_users_referral_code",
                table: "users",
                column: "ReferralCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_wallet_accounts_user_id",
                table: "wallet_accounts",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_wallet_ledger_entries_reference",
                table: "wallet_ledger_entries",
                column: "Reference");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_ledger_entries_user_id",
                table: "wallet_ledger_entries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_ledger_entries_wallet_account_id",
                table: "wallet_ledger_entries",
                column: "WalletAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wallet_ledger_entries");

            migrationBuilder.DropTable(
                name: "wallet_accounts");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
