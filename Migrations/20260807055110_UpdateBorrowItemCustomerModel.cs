using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace crud.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBorrowItemCustomerModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Borrows");

            migrationBuilder.DropColumn(
                name: "ItemName",
                table: "Borrows");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "Borrows");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Items",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CustomerID",
                table: "Borrows",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ItemID",
                table: "Borrows",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    CustomerID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.CustomerID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Borrows_CustomerID",
                table: "Borrows",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_Borrows_ItemID",
                table: "Borrows",
                column: "ItemID");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Borrows_Customers_CustomerID",
                table: "Borrows");

            migrationBuilder.DropForeignKey(
                name: "FK_Borrows_Items_ItemID",
                table: "Borrows");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Borrows_CustomerID",
                table: "Borrows");

            migrationBuilder.DropIndex(
                name: "IX_Borrows_ItemID",
                table: "Borrows");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "CustomerID",
                table: "Borrows");

            migrationBuilder.DropColumn(
                name: "ItemID",
                table: "Borrows");

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Borrows",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ItemName",
                table: "Borrows",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Size",
                table: "Borrows",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
