using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNonConformitaCampiEstesi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataEsito",
                table: "NonConformita",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Esito",
                table: "NonConformita",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoProblema",
                table: "NonConformita",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipologiaNc",
                table: "NonConformita",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataEsito",
                table: "NonConformita");

            migrationBuilder.DropColumn(
                name: "Esito",
                table: "NonConformita");

            migrationBuilder.DropColumn(
                name: "MotivoProblema",
                table: "NonConformita");

            migrationBuilder.DropColumn(
                name: "TipologiaNc",
                table: "NonConformita");
        }
    }
}
