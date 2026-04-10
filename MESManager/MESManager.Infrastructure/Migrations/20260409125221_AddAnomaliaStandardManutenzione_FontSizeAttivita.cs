using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnomaliaStandardManutenzione_FontSizeAttivita : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FontSize",
                table: "ManutenzioneAttivita",
                type: "int",
                nullable: false,
                defaultValue: 11);

            migrationBuilder.CreateTable(
                name: "AnomalieStandardManutenzione",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Testo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ordine = table.Column<int>(type: "int", nullable: false),
                    Attiva = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnomalieStandardManutenzione", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnomalieStandardManutenzione");

            migrationBuilder.DropColumn(
                name: "FontSize",
                table: "ManutenzioneAttivita");
        }
    }
}
