using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace crud.Migrations
{
    /// <inheritdoc />
    public partial class BorrowItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Borrows_Customers_CustomerID",
                table: "Borrows");

            migrationBuilder.DropForeignKey(
                name: "FK_Borrows_Items_ItemID",
                table: "Borrows");

            migrationBuilder.AddColumn<int>(
                name: "CustomerID1",
                table: "Borrows",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BorrowItems",
                columns: table => new
                {
                    BorrowItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BorrowID = table.Column<int>(type: "int", nullable: false),
                    ItemID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BorrowItems", x => x.BorrowItemID);
                    table.ForeignKey(
                        name: "FK_BorrowItems_Borrows_BorrowID",
                        column: x => x.BorrowID,
                        principalTable: "Borrows",
                        principalColumn: "BorrowID");
                    table.ForeignKey(
                        name: "FK_BorrowItems_Items_ItemID",
                        column: x => x.ItemID,
                        principalTable: "Items",
                        principalColumn: "ItemID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Borrows_CustomerID1",
                table: "Borrows",
                column: "CustomerID1");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowItems_BorrowID",
                table: "BorrowItems",
                column: "BorrowID");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowItems_ItemID",
                table: "BorrowItems",
                column: "ItemID");

            migrationBuilder.AddForeignKey(
                name: "FK_Borrows_Customers_CustomerID",
                table: "Borrows",
                column: "CustomerID",
                principalTable: "Customers",
                principalColumn: "CustomerID");

            migrationBuilder.AddForeignKey(
                name: "FK_Borrows_Customers_CustomerID1",
                table: "Borrows",
                column: "CustomerID1",
                principalTable: "Customers",
                principalColumn: "CustomerID");

            migrationBuilder.AddForeignKey(
                name: "FK_Borrows_Items_ItemID",
                table: "Borrows",
                column: "ItemID",
                principalTable: "Items",
                principalColumn: "ItemID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Borrows_Customers_CustomerID",
                table: "Borrows");

            migrationBuilder.DropForeignKey(
                name: "FK_Borrows_Customers_CustomerID1",
                table: "Borrows");

            migrationBuilder.DropForeignKey(
                name: "FK_Borrows_Items_ItemID",
                table: "Borrows");

            migrationBuilder.DropTable(
                name: "BorrowItems");

            migrationBuilder.DropIndex(
                name: "IX_Borrows_CustomerID1",
                table: "Borrows");

            migrationBuilder.DropColumn(
                name: "CustomerID1",
                table: "Borrows");

            migrationBuilder.AddForeignKey(
                name: "FK_Borrows_Customers_CustomerID",
                table: "Borrows",
                column: "CustomerID",
                principalTable: "Customers",
                principalColumn: "CustomerID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Borrows_Items_ItemID",
                table: "Borrows",
                column: "ItemID",
                principalTable: "Items",
                principalColumn: "ItemID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
