using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace crud.Migrations
{
    /// <inheritdoc />
    public partial class AddSimplePenalty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Penalties",
                columns: table => new
                {
                    PenaltyID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    BorrowID = table.Column<int>(type: "int", nullable: false),

                    DaysLate = table.Column<int>(type: "int", nullable: false),

                    PenaltyPerDay = table.Column<decimal>(
                        type: "decimal(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false),

                    PenaltyAmount = table.Column<decimal>(
                        type: "decimal(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_Penalties",
                        x => x.PenaltyID);

                    table.ForeignKey(
                        name: "FK_Penalties_Borrows_BorrowID",
                        column: x => x.BorrowID,
                        principalTable: "Borrows",
                        principalColumn: "BorrowID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Penalties_BorrowID",
                table: "Penalties",
                column: "BorrowID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Penalties");
        }
    }
}