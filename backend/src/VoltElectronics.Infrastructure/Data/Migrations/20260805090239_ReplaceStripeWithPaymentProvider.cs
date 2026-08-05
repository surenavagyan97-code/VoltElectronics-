using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltElectronics.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceStripeWithPaymentProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StripePaymentIntentId",
                table: "Orders",
                newName: "PaymentId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_StripePaymentIntentId",
                table: "Orders",
                newName: "IX_Orders_PaymentId");

            migrationBuilder.AddColumn<string>(
                name: "PaymentProvider",
                table: "Orders",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentProvider",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "PaymentId",
                table: "Orders",
                newName: "StripePaymentIntentId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_PaymentId",
                table: "Orders",
                newName: "IX_Orders_StripePaymentIntentId");
        }
    }
}
