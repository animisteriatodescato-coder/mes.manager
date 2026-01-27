using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUtentiAppEPreferenze : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UtentiApp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Attivo = table.Column<bool>(type: "bit", nullable: false),
                    Ordine = table.Column<int>(type: "int", nullable: false),
                    DataCreazione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UltimaModifica = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtentiApp", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PreferenzeUtente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UtenteAppId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Chiave = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ValoreJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataCreazione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UltimaModifica = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreferenzeUtente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreferenzeUtente_UtentiApp_UtenteAppId",
                        column: x => x.UtenteAppId,
                        principalTable: "UtentiApp",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PreferenzeUtente_UtenteAppId_Chiave",
                table: "PreferenzeUtente",
                columns: new[] { "UtenteAppId", "Chiave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UtentiApp_Nome",
                table: "UtentiApp",
                column: "Nome",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreferenzeUtente");

            migrationBuilder.DropTable(
                name: "UtentiApp");
        }
    }
}
