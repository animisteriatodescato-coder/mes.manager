using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddControlliQualitaInProcess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ControlloQualitaAttivita",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Dettaglio = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Ordine = table.Column<int>(type: "int", nullable: false),
                    Attiva = table.Column<bool>(type: "bit", nullable: false),
                    FontSize = table.Column<int>(type: "int", nullable: false),
                    QuandoNecessario = table.Column<bool>(type: "bit", nullable: false),
                    RichiedeNotaSeProblema = table.Column<bool>(type: "bit", nullable: false),
                    MacchinaCodiceFiltro = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlloQualitaAttivita", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ControlloQualitaSchede",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MacchinaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataEsecuzione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OperatoreId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    NomeOperatore = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Stato = table.Column<int>(type: "int", nullable: false),
                    DataChiusura = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlloQualitaSchede", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ControlloQualitaSchede_Macchine_MacchinaId",
                        column: x => x.MacchinaId,
                        principalTable: "Macchine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ControlloQualitaRighe",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchedaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttivitaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Esito = table.Column<int>(type: "int", nullable: false),
                    Commento = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DataUltimaModifica = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlloQualitaRighe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ControlloQualitaRighe_ControlloQualitaAttivita_AttivitaId",
                        column: x => x.AttivitaId,
                        principalTable: "ControlloQualitaAttivita",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ControlloQualitaRighe_ControlloQualitaSchede_SchedaId",
                        column: x => x.SchedaId,
                        principalTable: "ControlloQualitaSchede",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ControlloQualitaAttivita_Attiva",
                table: "ControlloQualitaAttivita",
                column: "Attiva");

            migrationBuilder.CreateIndex(
                name: "IX_ControlloQualitaAttivita_Ordine",
                table: "ControlloQualitaAttivita",
                column: "Ordine");

            migrationBuilder.CreateIndex(
                name: "IX_ControlloQualitaRighe_AttivitaId",
                table: "ControlloQualitaRighe",
                column: "AttivitaId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlloQualitaRighe_SchedaId",
                table: "ControlloQualitaRighe",
                column: "SchedaId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlloQualitaSchede_DataEsecuzione",
                table: "ControlloQualitaSchede",
                column: "DataEsecuzione");

            migrationBuilder.CreateIndex(
                name: "IX_ControlloQualitaSchede_MacchinaId",
                table: "ControlloQualitaSchede",
                column: "MacchinaId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlloQualitaSchede_MacchinaId_DataEsecuzione",
                table: "ControlloQualitaSchede",
                columns: new[] { "MacchinaId", "DataEsecuzione" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ControlloQualitaRighe");

            migrationBuilder.DropTable(
                name: "ControlloQualitaAttivita");

            migrationBuilder.DropTable(
                name: "ControlloQualitaSchede");
        }
    }
}
