using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Payment.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentProviderIntent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProviderIntentId",
                table: "Payments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProviderRequestedAt",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProviderIntentId",
                table: "Payments",
                column: "ProviderIntentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_ProviderIntentId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProviderIntentId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProviderRequestedAt",
                table: "Payments");
        }
    }
}
