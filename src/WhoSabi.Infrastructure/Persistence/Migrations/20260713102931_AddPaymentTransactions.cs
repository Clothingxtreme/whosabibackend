using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhoSabi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payment_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PendingSignupId = table.Column<Guid>(type: "uuid", nullable: true),
                    InternalReference = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ProviderReference = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Purpose = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AmountKobo = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PaymentUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payment_transactions_pending_signups_PendingSignupId",
                        column: x => x.PendingSignupId,
                        principalTable: "pending_signups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payment_transactions_pending_signup_id",
                table: "payment_transactions",
                column: "PendingSignupId");

            migrationBuilder.CreateIndex(
                name: "ux_payment_transactions_internal_reference",
                table: "payment_transactions",
                column: "InternalReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_payment_transactions_provider_reference",
                table: "payment_transactions",
                column: "ProviderReference",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_transactions");
        }
    }
}
