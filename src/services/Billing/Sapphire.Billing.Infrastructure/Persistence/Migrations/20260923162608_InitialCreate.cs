using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sapphire.Billing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "promocodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NormalizedCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    ValueCents = table.Column<long>(type: "bigint", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaxTotalUses = table.Column<int>(type: "integer", nullable: true),
                    MaxUsesPerUser = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UsedCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promocodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tariffs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    PricePerMinuteCents = table.Column<long>(type: "bigint", nullable: false),
                    PricePerHourCents = table.Column<long>(type: "bigint", nullable: false),
                    PackageDurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    PackageBonusMinutes = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tariffs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "wallets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    main_balance_cents = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    bonus_balance_cents = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WalletTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    AmountCents = table.Column<long>(type: "bigint", nullable: false),
                    BonusAmountCents = table.Column<long>(type: "bigint", nullable: false),
                    BalanceAfterCents = table.Column<long>(type: "bigint", nullable: false),
                    BonusBalanceAfterCents = table.Column<long>(type: "bigint", nullable: false),
                    Method = table.Column<string>(type: "text", nullable: true),
                    ReferenceId = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromocodeUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromocodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsedCount = table.Column<int>(type: "integer", nullable: false),
                    FirstUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromocodeUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromocodeUsages_promocodes_PromocodeId",
                        column: x => x.PromocodeId,
                        principalTable: "promocodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_promocodes_NormalizedCode",
                table: "promocodes",
                column: "NormalizedCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromocodeUsages_PromocodeId",
                table: "PromocodeUsages",
                column: "PromocodeId");

            migrationBuilder.CreateIndex(
                name: "IX_wallets_UserId",
                table: "wallets",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "PromocodeUsages");

            migrationBuilder.DropTable(
                name: "tariffs");

            migrationBuilder.DropTable(
                name: "wallets");

            migrationBuilder.DropTable(
                name: "WalletTransactions");

            migrationBuilder.DropTable(
                name: "promocodes");
        }
    }
}
