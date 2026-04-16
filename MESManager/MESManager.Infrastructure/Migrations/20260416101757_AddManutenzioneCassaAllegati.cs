using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddManutenzioneCassaAllegati : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManutenzioneCasseAllegati",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchedaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomeFile = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    PathFile = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TipoFile = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Documento"),
                    Estensione = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DimensioneBytes = table.Column<long>(type: "bigint", nullable: false),
                    DataCaricamento = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManutenzioneCasseAllegati", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManutenzioneCasseAllegati_ManutenzioneCasseSchede_SchedaId",
                        column: x => x.SchedaId,
                        principalTable: "ManutenzioneCasseSchede",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManutenzioneCasseAllegati_SchedaId",
                table: "ManutenzioneCasseAllegati",
                column: "SchedaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManutenzioneCasseAllegati");
        }
    }
}
