using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddManutenzioneCasse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManutenzioneCassaAttivita",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Ordine = table.Column<int>(type: "int", nullable: false),
                    Attiva = table.Column<bool>(type: "bit", nullable: false),
                    FontSize = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManutenzioneCassaAttivita", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManutenzioneCasseSchede",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodiceCassa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DataEsecuzione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OperatoreId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    NomeOperatore = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Stato = table.Column<int>(type: "int", nullable: false),
                    DataChiusura = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManutenzioneCasseSchede", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManutenzioneCasseRighe",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchedaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttivitaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Esito = table.Column<int>(type: "int", nullable: false),
                    Commento = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManutenzioneCasseRighe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManutenzioneCasseRighe_ManutenzioneCassaAttivita_AttivitaId",
                        column: x => x.AttivitaId,
                        principalTable: "ManutenzioneCassaAttivita",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManutenzioneCasseRighe_ManutenzioneCasseSchede_SchedaId",
                        column: x => x.SchedaId,
                        principalTable: "ManutenzioneCasseSchede",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManutenzioneCassaAttivita_Attiva",
                table: "ManutenzioneCassaAttivita",
                column: "Attiva");

            migrationBuilder.CreateIndex(
                name: "IX_ManutenzioneCasseRighe_AttivitaId",
                table: "ManutenzioneCasseRighe",
                column: "AttivitaId");

            migrationBuilder.CreateIndex(
                name: "IX_ManutenzioneCasseRighe_SchedaId",
                table: "ManutenzioneCasseRighe",
                column: "SchedaId");

            migrationBuilder.CreateIndex(
                name: "IX_ManutenzioneCasseSchede_CodiceCassa",
                table: "ManutenzioneCasseSchede",
                column: "CodiceCassa");

            migrationBuilder.CreateIndex(
                name: "IX_ManutenzioneCasseSchede_DataEsecuzione",
                table: "ManutenzioneCasseSchede",
                column: "DataEsecuzione");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManutenzioneCasseRighe");

            migrationBuilder.DropTable(
                name: "ManutenzioneCassaAttivita");

            migrationBuilder.DropTable(
                name: "ManutenzioneCasseSchede");
        }
    }
}
