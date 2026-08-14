using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltElectronics.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContentPages_Key",
                table: "ContentPages");

            // "en" (not the scaffolded "") so rows written before localization keep resolving as
            // the default language.
            migrationBuilder.AddColumn<string>(
                name: "Lang",
                table: "ContentPages",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "en");

            migrationBuilder.CreateTable(
                name: "ProductTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Lang = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductTranslations_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentPages_Key_Lang",
                table: "ContentPages",
                columns: new[] { "Key", "Lang" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductTranslations_ProductId_Lang",
                table: "ProductTranslations",
                columns: new[] { "ProductId", "Lang" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductTranslations");

            migrationBuilder.DropIndex(
                name: "IX_ContentPages_Key_Lang",
                table: "ContentPages");

            migrationBuilder.DropColumn(
                name: "Lang",
                table: "ContentPages");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPages_Key",
                table: "ContentPages",
                column: "Key",
                unique: true);
        }
    }
}
