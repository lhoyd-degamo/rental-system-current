using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace crud.Migrations
{
    public partial class AddReturnInspectionFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(name: "ActualReturnDate", table: "Borrows", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ItemCondition", table: "Borrows", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "DamageDescription", table: "Borrows", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<decimal>(name: "DamagePenalty", table: "Borrows", type: "decimal(18,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(name: "PenaltyAmount", table: "Borrows", type: "decimal(18,2)", nullable: false, defaultValue: 0m);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ActualReturnDate", table: "Borrows");
            migrationBuilder.DropColumn(name: "ItemCondition", table: "Borrows");
            migrationBuilder.DropColumn(name: "DamageDescription", table: "Borrows");
            migrationBuilder.DropColumn(name: "DamagePenalty", table: "Borrows");
            migrationBuilder.DropColumn(name: "PenaltyAmount", table: "Borrows");
        }
    }
}