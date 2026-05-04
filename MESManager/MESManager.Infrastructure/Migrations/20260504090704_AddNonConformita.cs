using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNonConformita : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NonConformita",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodiceArticolo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DescrizioneArticolo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Cliente = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DataSegnalazione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Gravita = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AzioneCorrettiva = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Stato = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatoDa = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatoIl = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModificatoDa = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModificatoIl = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataChiusura = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonConformita", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NonConformita");
        }
    }
}
