using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace ECommerce.Payment.Data.Migrations
{
    [DbContext(typeof(PaymentDbContext))]
    [Migration("20260713000000_PersistPaymentCustomer")]
    public partial class PersistPaymentCustomer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "Payments",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "Payments",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CustomerEmail", table: "Payments");
            migrationBuilder.DropColumn(name: "CustomerId", table: "Payments");
        }
    }
}
