using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace crud.Migrations
{
    /// <inheritdoc />
    public partial class MoveIDInformationToCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove the old duplicate CustomerID1 relationship
            migrationBuilder.DropForeignKey(
                name: "FK_Borrows_Customers_CustomerID1",
                table: "Borrows");

            migrationBuilder.DropIndex(
                name: "IX_Borrows_CustomerID1",
                table: "Borrows");

            migrationBuilder.DropColumn(
                name: "CustomerID1",
                table: "Borrows");


            // Add ID information to Customers
            migrationBuilder.AddColumn<string>(
                name: "IDNumber",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IDType",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");


            // Move existing ID information from Borrows to Customers
            // The most recent Borrow record is used when a customer
            // has multiple borrow records.
            migrationBuilder.Sql(@"
                UPDATE c
                SET
                    c.IDType = b.IDType,
                    c.IDNumber = b.IDNumber
                FROM Customers c
                INNER JOIN
                (
                    SELECT
                        CustomerID,
                        IDType,
                        IDNumber,
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY CustomerID
                            ORDER BY BorrowID DESC
                        ) AS RowNum
                    FROM Borrows
                    WHERE IDType IS NOT NULL
                       OR IDNumber IS NOT NULL
                ) b
                    ON c.CustomerID = b.CustomerID
                WHERE b.RowNum = 1;
            ");


            // Remove the old ID fields from Borrows
            migrationBuilder.DropColumn(
                name: "IDNumber",
                table: "Borrows");

            migrationBuilder.DropColumn(
                name: "IDType",
                table: "Borrows");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the old ID fields on Borrows
            migrationBuilder.AddColumn<string>(
                name: "IDNumber",
                table: "Borrows",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IDType",
                table: "Borrows",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");


            // Copy customer ID information back to Borrows
            migrationBuilder.Sql(@"
                UPDATE b
                SET
                    b.IDType = c.IDType,
                    b.IDNumber = c.IDNumber
                FROM Borrows b
                INNER JOIN Customers c
                    ON b.CustomerID = c.CustomerID;
            ");


            // Remove ID information from Customers
            migrationBuilder.DropColumn(
                name: "IDNumber",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IDType",
                table: "Customers");


            // Restore the old CustomerID1 relationship
            migrationBuilder.AddColumn<int>(
                name: "CustomerID1",
                table: "Borrows",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Borrows_CustomerID1",
                table: "Borrows",
                column: "CustomerID1");

            migrationBuilder.AddForeignKey(
                name: "FK_Borrows_Customers_CustomerID1",
                table: "Borrows",
                column: "CustomerID1",
                principalTable: "Customers",
                principalColumn: "CustomerID");
        }
    }
}