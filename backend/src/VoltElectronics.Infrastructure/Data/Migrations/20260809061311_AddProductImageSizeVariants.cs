using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltElectronics.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductImageSizeVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CardUrl",
                table: "ProductImages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ThumbUrl",
                table: "ProductImages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CardUrl",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "ThumbUrl",
                table: "ProductImages");
        }
    }
}
