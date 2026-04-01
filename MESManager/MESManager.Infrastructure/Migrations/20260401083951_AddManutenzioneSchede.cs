using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddManutenzioneSchede : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManutenzioneAttivita",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoFrequenza = table.Column<int>(type: "int", nullable: false),
                    Ordine = table.Column<int>(type: "int", nullable: false),
                    Attiva = table.Column<bool>(type: "bit", nullable: false),
                    CicliSogliaPLC = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManutenzioneAttivita", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManutenzioneSchede",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MacchinaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoFrequenza = table.Column<int>(type: "int", nullable: false),
                    DataEsecuzione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OperatoreId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NomeOperatore = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Stato = table.Column<int>(type: "int", nullable: false),
                    DataChiusura = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManutenzioneSchede", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManutenzioneSchede_Macchine_MacchinaId",
                        column: x => x.MacchinaId,
                        principalTable: "Macchine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManutenzioneRighe",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchedaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttivitaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Esito = table.Column<int>(type: "int", nullable: false),
                    Commento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FotoPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CicloMacchinaAlEsecuzione = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManutenzioneRighe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManutenzioneRighe_ManutenzioneAttivita_AttivitaId",
                        column: x => x.AttivitaId,
                        principalTable: "ManutenzioneAttivita",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManutenzioneRighe_ManutenzioneSchede_SchedaId",
                        column: x => x.SchedaId,
                        principalTable: "ManutenzioneSchede",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManutenzioneAttivita_TipoFrequenza",
                table: "ManutenzioneAttivita",
                column: "TipoFrequenza");

            migrationBuilder.CreateIndex(
                name: "IX_ManutenzioneRighe_AttivitaId",
                table: "ManutenzioneRighe",
                column: "AttivitaId");

            migrationBuilder.CreateIndex(
                name: "IX_ManutenzioneRighe_SchedaId",
                table: "ManutenzioneRighe",
                column: "SchedaId");

            migrationBuilder.CreateIndex(
                name: "IX_ManutenzioneSchede_DataEsecuzione",
                table: "ManutenzioneSchede",
                column: "DataEsecuzione");

            migrationBuilder.CreateIndex(
                name: "IX_ManutenzioneSchede_MacchinaId",
                table: "ManutenzioneSchede",
                column: "MacchinaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManutenzioneRighe");

            migrationBuilder.DropTable(
                name: "ManutenzioneAttivita");

            migrationBuilder.DropTable(
                name: "ManutenzioneSchede");
        }
    }
}
