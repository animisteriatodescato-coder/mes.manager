using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UnificazioneUtentiApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PreferenzeUtente_UtentiApp_UtenteAppId",
                table: "PreferenzeUtente");

            migrationBuilder.DropTable(
                name: "UtentiApp");

            migrationBuilder.DropIndex(
                name: "IX_PreferenzeUtente_UtenteAppId_Chiave",
                table: "PreferenzeUtente");

            migrationBuilder.DropColumn(
                name: "UtenteAppId",
                table: "PreferenzeUtente");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "PreferenzeUtente",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Attivo",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Colore",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataCreazione",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Nome",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Ordine",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Rimuove righe orfane (UserId = '') create per pre-esistenti record senza utente associato
            migrationBuilder.Sql("DELETE FROM [PreferenzeUtente] WHERE [UserId] = ''");

            migrationBuilder.CreateIndex(
                name: "IX_PreferenzeUtente_UserId_Chiave",
                table: "PreferenzeUtente",
                columns: new[] { "UserId", "Chiave" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PreferenzeUtente_UserId_Chiave",
                table: "PreferenzeUtente");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PreferenzeUtente");

            migrationBuilder.DropColumn(
                name: "Attivo",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Colore",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DataCreazione",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Nome",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Ordine",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<Guid>(
                name: "UtenteAppId",
                table: "PreferenzeUtente",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "UtentiApp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Attivo = table.Column<bool>(type: "bit", nullable: false),
                    Colore = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataCreazione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Ordine = table.Column<int>(type: "int", nullable: false),
                    UltimaModifica = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtentiApp", x => x.Id);
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

            migrationBuilder.AddForeignKey(
                name: "FK_PreferenzeUtente_UtentiApp_UtenteAppId",
                table: "PreferenzeUtente",
                column: "UtenteAppId",
                principalTable: "UtentiApp",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
