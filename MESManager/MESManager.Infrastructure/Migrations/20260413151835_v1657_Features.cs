using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v1657_Features : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CommessaId",
                table: "Preventivi",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailDestinatario",
                table: "Preventivi",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailInviatoIl",
                table: "Preventivi",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoteInterne",
                table: "Preventivi",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Sconto",
                table: "Preventivi",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "PreventivoRevisioni",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreventivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroRevisione = table.Column<int>(type: "int", nullable: false),
                    DataRevisione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DtoJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NoteRevisione = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreventivoRevisioni", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreventivoRevisioni_Preventivi_PreventivoId",
                        column: x => x.PreventivoId,
                        principalTable: "Preventivi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreventivoTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DataCreazione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ParametriJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreventivoTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PreventivoRevisioni_PreventivoId",
                table: "PreventivoRevisioni",
                column: "PreventivoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreventivoRevisioni");

            migrationBuilder.DropTable(
                name: "PreventivoTemplates");

            migrationBuilder.DropColumn(
                name: "CommessaId",
                table: "Preventivi");

            migrationBuilder.DropColumn(
                name: "EmailDestinatario",
                table: "Preventivi");

            migrationBuilder.DropColumn(
                name: "EmailInviatoIl",
                table: "Preventivi");

            migrationBuilder.DropColumn(
                name: "NoteInterne",
                table: "Preventivi");

            migrationBuilder.DropColumn(
                name: "Sconto",
                table: "Preventivi");
        }
    }
}
