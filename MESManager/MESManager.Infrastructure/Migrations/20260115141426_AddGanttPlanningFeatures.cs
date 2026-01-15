using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGanttPlanningFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataFinePrevisione",
                table: "Commesse",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataFineProduzione",
                table: "Commesse",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataInizioPrevisione",
                table: "Commesse",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataInizioProduzione",
                table: "Commesse",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumeroFigure",
                table: "Articoli",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TempoCiclo",
                table: "Articoli",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ImpostazioniProduzione",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TempoSetupMinuti = table.Column<int>(type: "int", nullable: false),
                    OreLavorativeGiornaliere = table.Column<int>(type: "int", nullable: false),
                    GiorniLavorativiSettimanali = table.Column<int>(type: "int", nullable: false),
                    UltimaModifica = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpostazioniProduzione", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImpostazioniProduzione");

            migrationBuilder.DropColumn(
                name: "DataFinePrevisione",
                table: "Commesse");

            migrationBuilder.DropColumn(
                name: "DataFineProduzione",
                table: "Commesse");

            migrationBuilder.DropColumn(
                name: "DataInizioPrevisione",
                table: "Commesse");

            migrationBuilder.DropColumn(
                name: "DataInizioProduzione",
                table: "Commesse");

            migrationBuilder.DropColumn(
                name: "NumeroFigure",
                table: "Articoli");

            migrationBuilder.DropColumn(
                name: "TempoCiclo",
                table: "Articoli");
        }
    }
}
