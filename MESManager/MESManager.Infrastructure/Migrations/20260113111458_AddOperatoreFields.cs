using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOperatoreFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Attivo",
                table: "Operatori",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataAssunzione",
                table: "Operatori",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataLicenziamento",
                table: "Operatori",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Attivo",
                table: "Operatori");

            migrationBuilder.DropColumn(
                name: "DataAssunzione",
                table: "Operatori");

            migrationBuilder.DropColumn(
                name: "DataLicenziamento",
                table: "Operatori");
        }
    }
}
