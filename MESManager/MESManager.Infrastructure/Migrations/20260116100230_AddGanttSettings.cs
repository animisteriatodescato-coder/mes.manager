using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGanttSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AttivaInGantt",
                table: "Macchine",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OrdineVisualizazione",
                table: "Macchine",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CalendarioLavoro",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Lunedi = table.Column<bool>(type: "bit", nullable: false),
                    Martedi = table.Column<bool>(type: "bit", nullable: false),
                    Mercoledi = table.Column<bool>(type: "bit", nullable: false),
                    Giovedi = table.Column<bool>(type: "bit", nullable: false),
                    Venerdi = table.Column<bool>(type: "bit", nullable: false),
                    Sabato = table.Column<bool>(type: "bit", nullable: false),
                    Domenica = table.Column<bool>(type: "bit", nullable: false),
                    OraInizio = table.Column<TimeOnly>(type: "time", nullable: false),
                    OraFine = table.Column<TimeOnly>(type: "time", nullable: false),
                    DataCreazione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataModifica = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarioLavoro", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImpostazioniGantt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AbilitaTempoAttrezzaggio = table.Column<bool>(type: "bit", nullable: false),
                    TempoAttrezzaggioMinutiDefault = table.Column<int>(type: "int", nullable: false),
                    DataCreazione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataModifica = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpostazioniGantt", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarioLavoro");

            migrationBuilder.DropTable(
                name: "ImpostazioniGantt");

            migrationBuilder.DropColumn(
                name: "AttivaInGantt",
                table: "Macchine");

            migrationBuilder.DropColumn(
                name: "OrdineVisualizazione",
                table: "Macchine");
        }
    }
}
