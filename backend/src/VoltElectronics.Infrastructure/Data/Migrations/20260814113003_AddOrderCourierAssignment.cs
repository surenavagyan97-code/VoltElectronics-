using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltElectronics.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCourierAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedCourierId",
                table: "Orders",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_AssignedCourierId",
                table: "Orders",
                column: "AssignedCourierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_AssignedCourierId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AssignedCourierId",
                table: "Orders");
        }
    }
}
