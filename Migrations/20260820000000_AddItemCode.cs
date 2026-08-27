using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace crud.Migrations
{
    /// <inheritdoc />
    public partial class AddItemCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ItemCode",
                table: "Items",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            // Backfill a code for any items that already exist
            // so the unique index below does not collide on empty strings.
            migrationBuilder.Sql(@"
                UPDATE Items
                SET ItemCode = 'ITM' + RIGHT('000' + CAST(ItemID AS varchar(10)), 3)
                WHERE ItemCode = '' OR ItemCode IS NULL;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Items_ItemCode",
                table: "Items",
                column: "ItemCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_ItemCode",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ItemCode",
                table: "Items");
        }
    }
}
