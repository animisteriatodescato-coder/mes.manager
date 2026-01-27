using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAllegatiArticoli : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AllegatiArticoli",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Archivio = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IdArchivio = table.Column<int>(type: "int", nullable: false),
                    CodiceArticolo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PathFile = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NomeFile = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Descrizione = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Priorita = table.Column<int>(type: "int", nullable: false),
                    TipoFile = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Estensione = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DimensioneBytes = table.Column<long>(type: "bigint", nullable: true),
                    DataImportazione = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataCreazione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImportatoDaGantt = table.Column<bool>(type: "bit", nullable: false),
                    IdGanttOriginale = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllegatiArticoli", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllegatiArticoli_Archivio_IdArchivio",
                table: "AllegatiArticoli",
                columns: new[] { "Archivio", "IdArchivio" });

            migrationBuilder.CreateIndex(
                name: "IX_AllegatiArticoli_CodiceArticolo",
                table: "AllegatiArticoli",
                column: "CodiceArticolo");

            migrationBuilder.CreateIndex(
                name: "IX_AllegatiArticoli_IdGanttOriginale",
                table: "AllegatiArticoli",
                column: "IdGanttOriginale");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllegatiArticoli");
        }
    }
}
