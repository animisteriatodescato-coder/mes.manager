using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Articoli",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codice = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articoli", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clienti",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codice = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RagioneSociale = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clienti", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogEventi",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataOra = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Utente = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Azione = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Entita = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdEntita = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValorePrecedenteJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValoreSuccessivoJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEventi", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Macchine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codice = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Stato = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Macchine", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Operatori",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Matricola = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cognome = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Operatori", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ricette",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArticoloId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ricette", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ricette_Articoli_ArticoloId",
                        column: x => x.ArticoloId,
                        principalTable: "Articoli",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Commesse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codice = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ArticoloId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuantitaRichiesta = table.Column<int>(type: "int", nullable: false),
                    Stato = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commesse", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Commesse_Articoli_ArticoloId",
                        column: x => x.ArticoloId,
                        principalTable: "Articoli",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Commesse_Clienti_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clienti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurazioniPLC",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MacchinaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomeParametro = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Indirizzo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoDato = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurazioniPLC", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfigurazioniPLC_Macchine_MacchinaId",
                        column: x => x.MacchinaId,
                        principalTable: "Macchine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventiPLC",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MacchinaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataOra = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TipoEvento = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventiPLC", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventiPLC_Macchine_MacchinaId",
                        column: x => x.MacchinaId,
                        principalTable: "Macchine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Manutenzioni",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MacchinaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataPrevista = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataEsecuzione = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manutenzioni", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Manutenzioni_Macchine_MacchinaId",
                        column: x => x.MacchinaId,
                        principalTable: "Macchine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PLCRealtime",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MacchinaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataUltimoAggiornamento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValoreGenerico = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PLCRealtime", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PLCRealtime_Macchine_MacchinaId",
                        column: x => x.MacchinaId,
                        principalTable: "Macchine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PLCStorico",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MacchinaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataOra = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Dati = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PLCStorico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PLCStorico_Macchine_MacchinaId",
                        column: x => x.MacchinaId,
                        principalTable: "Macchine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParametriRicetta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RicettaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomeParametro = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Valore = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitaMisura = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametriRicetta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParametriRicetta_Ricette_RicettaId",
                        column: x => x.RicettaId,
                        principalTable: "Ricette",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Articoli_Codice",
                table: "Articoli",
                column: "Codice");

            migrationBuilder.CreateIndex(
                name: "IX_Clienti_Codice",
                table: "Clienti",
                column: "Codice");

            migrationBuilder.CreateIndex(
                name: "IX_Commesse_ArticoloId",
                table: "Commesse",
                column: "ArticoloId");

            migrationBuilder.CreateIndex(
                name: "IX_Commesse_ClienteId",
                table: "Commesse",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Commesse_Codice",
                table: "Commesse",
                column: "Codice");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurazioniPLC_MacchinaId",
                table: "ConfigurazioniPLC",
                column: "MacchinaId");

            migrationBuilder.CreateIndex(
                name: "IX_EventiPLC_MacchinaId",
                table: "EventiPLC",
                column: "MacchinaId");

            migrationBuilder.CreateIndex(
                name: "IX_Macchine_Codice",
                table: "Macchine",
                column: "Codice");

            migrationBuilder.CreateIndex(
                name: "IX_Manutenzioni_MacchinaId",
                table: "Manutenzioni",
                column: "MacchinaId");

            migrationBuilder.CreateIndex(
                name: "IX_ParametriRicetta_RicettaId",
                table: "ParametriRicetta",
                column: "RicettaId");

            migrationBuilder.CreateIndex(
                name: "IX_PLCRealtime_MacchinaId",
                table: "PLCRealtime",
                column: "MacchinaId");

            migrationBuilder.CreateIndex(
                name: "IX_PLCStorico_MacchinaId",
                table: "PLCStorico",
                column: "MacchinaId");

            migrationBuilder.CreateIndex(
                name: "IX_Ricette_ArticoloId",
                table: "Ricette",
                column: "ArticoloId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Commesse");

            migrationBuilder.DropTable(
                name: "ConfigurazioniPLC");

            migrationBuilder.DropTable(
                name: "EventiPLC");

            migrationBuilder.DropTable(
                name: "LogEventi");

            migrationBuilder.DropTable(
                name: "Manutenzioni");

            migrationBuilder.DropTable(
                name: "Operatori");

            migrationBuilder.DropTable(
                name: "ParametriRicetta");

            migrationBuilder.DropTable(
                name: "PLCRealtime");

            migrationBuilder.DropTable(
                name: "PLCStorico");

            migrationBuilder.DropTable(
                name: "Clienti");

            migrationBuilder.DropTable(
                name: "Ricette");

            migrationBuilder.DropTable(
                name: "Macchine");

            migrationBuilder.DropTable(
                name: "Articoli");
        }
    }
}
