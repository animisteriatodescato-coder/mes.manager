using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPreventivi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PreventivoTipiSabbia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codice = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Famiglia = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EuroOra = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    PrezzoKg = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    SpariDefault = table.Column<int>(type: "int", nullable: false),
                    Attivo = table.Column<bool>(type: "bit", nullable: false),
                    Ordine = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreventivoTipiSabbia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PreventivoTipiVernice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codice = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Famiglia = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PrezzoKg = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    PercentualeApplicazione = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Attivo = table.Column<bool>(type: "bit", nullable: false),
                    Ordine = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreventivoTipiVernice", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Preventivi",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataCreazione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Cliente = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CodiceArticolo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NoteCliente = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoSabbiaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SabbiaSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EuroOraSabbia = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    PrezzoSabbiaKg = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    Figure = table.Column<int>(type: "int", nullable: false),
                    PesoAnima = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    Lotto = table.Column<int>(type: "int", nullable: false),
                    SpariOrari = table.Column<int>(type: "int", nullable: false),
                    CostoAttrezzatura = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    VerniciaturaRichiesta = table.Column<bool>(type: "bit", nullable: false),
                    TipoVerniceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VerniceSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CostoVerniceKg = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    PercentualeVernice = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    VerniciaturaPzOra = table.Column<int>(type: "int", nullable: false),
                    EuroOraVerniciatura = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IncollaggioRichiesto = table.Column<bool>(type: "bit", nullable: false),
                    EuroOraIncollaggio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IncollaggioPzOra = table.Column<int>(type: "int", nullable: false),
                    ImballaggioRichiesto = table.Column<bool>(type: "bit", nullable: false),
                    EuroOraImballaggio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ImballaggioPzOra = table.Column<int>(type: "int", nullable: false),
                    CalcCostoAnima = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    CalcVerniciaturaTot = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    CalcPrezzoVendita = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    Stato = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "InAttesa")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Preventivi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Preventivi_PreventivoTipiSabbia_TipoSabbiaId",
                        column: x => x.TipoSabbiaId,
                        principalTable: "PreventivoTipiSabbia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Preventivi_PreventivoTipiVernice_TipoVerniceId",
                        column: x => x.TipoVerniceId,
                        principalTable: "PreventivoTipiVernice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Preventivi_Cliente",
                table: "Preventivi",
                column: "Cliente");

            migrationBuilder.CreateIndex(
                name: "IX_Preventivi_DataCreazione",
                table: "Preventivi",
                column: "DataCreazione");

            migrationBuilder.CreateIndex(
                name: "IX_Preventivi_TipoSabbiaId",
                table: "Preventivi",
                column: "TipoSabbiaId");

            migrationBuilder.CreateIndex(
                name: "IX_Preventivi_TipoVerniceId",
                table: "Preventivi",
                column: "TipoVerniceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Preventivi");

            migrationBuilder.DropTable(
                name: "PreventivoTipiSabbia");

            migrationBuilder.DropTable(
                name: "PreventivoTipiVernice");
        }
    }
}
