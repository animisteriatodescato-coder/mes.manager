using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFotovoltaico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FotovoltaicoRealtime",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UltimoAggiornamento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConnessioneOk = table.Column<bool>(type: "bit", nullable: false),
                    ErroreConnessione = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PotenzaAttuale_kW = table.Column<double>(type: "float", nullable: false),
                    EnergiaOggi_kWh = table.Column<double>(type: "float", nullable: false),
                    EnergiaAccumulata_kWh = table.Column<double>(type: "float", nullable: false),
                    TensioneStringa_V = table.Column<double>(type: "float", nullable: false),
                    CorrenteStringa_A = table.Column<double>(type: "float", nullable: false),
                    TensioneRete_V = table.Column<double>(type: "float", nullable: false),
                    TemperaturaInterna_C = table.Column<double>(type: "float", nullable: false),
                    StatoInverter = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatoCodice = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FotovoltaicoRealtime", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FotovoltaicoStorico",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PotenzaMedia_kW = table.Column<double>(type: "float", nullable: false),
                    PotenzaMassima_kW = table.Column<double>(type: "float", nullable: false),
                    EnergiaOra_kWh = table.Column<double>(type: "float", nullable: false),
                    EnergiaAccumulata_kWh = table.Column<double>(type: "float", nullable: false),
                    StatoInverter = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FotovoltaicoStorico", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FotovoltaicoRealtime");

            migrationBuilder.DropTable(
                name: "FotovoltaicoStorico");
        }
    }
}
