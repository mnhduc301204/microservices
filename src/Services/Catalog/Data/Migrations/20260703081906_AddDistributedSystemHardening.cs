using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Catalog.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDistributedSystemHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessage_ProcessedAt",
                table: "OutboxMessage");

            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "OutboxMessage",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeadLetter",
                table: "OutboxMessage",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockedAt",
                table: "OutboxMessage",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockedBy",
                table: "OutboxMessage",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextAttemptAt",
                table: "OutboxMessage",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IdempotencyRecord",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StatusCode = table.Column<int>(type: "integer", nullable: false),
                    ResponseBody = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecord", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_ProcessedAt_NextAttemptAt_IsDeadLetter",
                table: "OutboxMessage",
                columns: new[] { "ProcessedAt", "NextAttemptAt", "IsDeadLetter" });

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecord_ServiceName_IdempotencyKey",
                table: "IdempotencyRecord",
                columns: new[] { "ServiceName", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdempotencyRecord");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessage_ProcessedAt_NextAttemptAt_IsDeadLetter",
                table: "OutboxMessage");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "OutboxMessage");

            migrationBuilder.DropColumn(
                name: "IsDeadLetter",
                table: "OutboxMessage");

            migrationBuilder.DropColumn(
                name: "LockedAt",
                table: "OutboxMessage");

            migrationBuilder.DropColumn(
                name: "LockedBy",
                table: "OutboxMessage");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "OutboxMessage");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_ProcessedAt",
                table: "OutboxMessage",
                column: "ProcessedAt");
        }
    }
}
