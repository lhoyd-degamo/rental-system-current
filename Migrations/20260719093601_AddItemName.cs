using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace crud.Migrations
{
    /// <inheritdoc />
    public partial class AddItemName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ItemName",
                table: "Items",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Items_CatID",
                table: "Items",
                column: "CatID");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Categories_CatID",
                table: "Items",
                column: "CatID",
                principalTable: "Categories",
                principalColumn: "CatID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Categories_CatID",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_CatID",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ItemName",
                table: "Items");
        }
    }
}
