using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStatoProgrammaAndStorico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataCambioStatoProgramma",
                table: "Commesse",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatoProgramma",
                table: "Commesse",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "StoricoProgrammazione",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommessaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatoPrecedente = table.Column<int>(type: "int", nullable: true),
                    StatoNuovo = table.Column<int>(type: "int", nullable: false),
                    DataModifica = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UtenteModifica = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoricoProgrammazione", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoricoProgrammazione_Commesse_CommessaId",
                        column: x => x.CommessaId,
                        principalTable: "Commesse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Commesse_StatoProgramma",
                table: "Commesse",
                column: "StatoProgramma");

            migrationBuilder.CreateIndex(
                name: "IX_StoricoProgrammazione_CommessaId",
                table: "StoricoProgrammazione",
                column: "CommessaId");

            migrationBuilder.CreateIndex(
                name: "IX_StoricoProgrammazione_DataModifica",
                table: "StoricoProgrammazione",
                column: "DataModifica");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoricoProgrammazione");

            migrationBuilder.DropIndex(
                name: "IX_Commesse_StatoProgramma",
                table: "Commesse");

            migrationBuilder.DropColumn(
                name: "DataCambioStatoProgramma",
                table: "Commesse");

            migrationBuilder.DropColumn(
                name: "StatoProgramma",
                table: "Commesse");
        }
    }
}
