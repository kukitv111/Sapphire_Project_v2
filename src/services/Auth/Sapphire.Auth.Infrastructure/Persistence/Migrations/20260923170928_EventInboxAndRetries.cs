using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sapphire.Auth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EventInboxAndRetries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "dead_letter_at",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_attempt_at",
                table: "outbox_messages",
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_messages");

            migrationBuilder.DropColumn(
                name: "dead_letter_at",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "next_attempt_at",
                table: "outbox_messages");
        }
    }
}
