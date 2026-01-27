using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnimeWithFullSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Anime",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodiceArticolo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DescrizioneArticolo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitaMisura = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Larghezza = table.Column<int>(type: "int", nullable: true),
                    Altezza = table.Column<int>(type: "int", nullable: true),
                    Profondita = table.Column<int>(type: "int", nullable: true),
                    Imballo = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Peso = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ubicazione = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ciclo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodiceCassa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodiceAnime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdArticolo = table.Column<int>(type: "int", nullable: true),
                    DataModificaRecord = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MacchineSuDisponibili = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrasmettiTutto = table.Column<bool>(type: "bit", nullable: false),
                    Allegato = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UtenteModificaRecord = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataImportazione = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anime", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Anime");
        }
    }
}
