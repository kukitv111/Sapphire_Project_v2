using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sapphire.Billing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PrepaidEntitlementsAndInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "PackagePriceCents",
                table: "tariffs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeadLetterAt",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptAt",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "session_reservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntitlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservedCents = table.Column<long>(type: "bigint", nullable: false),
                    ChargedCents = table.Column<long>(type: "bigint", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlannedEndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_reservations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tariff_entitlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TariffId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    PricePerMinuteCents = table.Column<long>(type: "bigint", nullable: false),
                    PricePerHourCents = table.Column<long>(type: "bigint", nullable: false),
                    PackagePriceCents = table.Column<long>(type: "bigint", nullable: false),
                    PackageMinutes = table.Column<int>(type: "integer", nullable: false),
                    PurchasedCents = table.Column<long>(type: "bigint", nullable: false),
                    AvailableCents = table.Column<long>(type: "bigint", nullable: false),
                    ReservedCents = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tariff_entitlements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_session_reservations_SessionId",
                table: "session_reservations",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tariff_entitlements_UserId_IdempotencyKey",
                table: "tariff_entitlements",
                columns: new[] { "UserId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_messages");

            migrationBuilder.DropTable(
                name: "session_reservations");

            migrationBuilder.DropTable(
                name: "tariff_entitlements");

            migrationBuilder.DropColumn(
                name: "PackagePriceCents",
                table: "tariffs");

            migrationBuilder.DropColumn(
                name: "DeadLetterAt",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "OutboxMessages");
        }
    }
}
