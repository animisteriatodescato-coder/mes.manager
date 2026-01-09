using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnsureSyncTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clienti_Codice",
                table: "Clienti");

            migrationBuilder.DropIndex(
                name: "IX_Articoli_Codice",
                table: "Articoli");

            migrationBuilder.AlterColumn<decimal>(
                name: "QuantitaRichiesta",
                table: "Commesse",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<Guid>(
                name: "ArticoloId",
                table: "Commesse",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataConsegna",
                table: "Commesse",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RiferimentoOrdineCliente",
                table: "Commesse",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TimestampSync",
                table: "Commesse",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaModifica",
                table: "Commesse",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "Attivo",
                table: "Clienti",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Clienti",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Clienti",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TimestampSync",
                table: "Clienti",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaModifica",
                table: "Clienti",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "Attivo",
                table: "Articoli",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Prezzo",
                table: "Articoli",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "TimestampSync",
                table: "Articoli",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaModifica",
                table: "Articoli",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "LogSync",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataOra = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modulo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nuovi = table.Column<int>(type: "int", nullable: false),
                    Aggiornati = table.Column<int>(type: "int", nullable: false),
                    Ignorati = table.Column<int>(type: "int", nullable: false),
                    Errori = table.Column<int>(type: "int", nullable: false),
                    MessaggioErrore = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileBackupPath = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogSync", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Modulo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UltimaSyncRiuscita = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clienti_Codice",
                table: "Clienti",
                column: "Codice",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Articoli_Codice",
                table: "Articoli",
                column: "Codice",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncStates_Modulo",
                table: "SyncStates",
                column: "Modulo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogSync");

            migrationBuilder.DropTable(
                name: "SyncStates");

            migrationBuilder.DropIndex(
                name: "IX_Clienti_Codice",
                table: "Clienti");

            migrationBuilder.DropIndex(
                name: "IX_Articoli_Codice",
                table: "Articoli");

            migrationBuilder.DropColumn(
                name: "DataConsegna",
                table: "Commesse");

            migrationBuilder.DropColumn(
                name: "RiferimentoOrdineCliente",
                table: "Commesse");

            migrationBuilder.DropColumn(
                name: "TimestampSync",
                table: "Commesse");

            migrationBuilder.DropColumn(
                name: "UltimaModifica",
                table: "Commesse");

            migrationBuilder.DropColumn(
                name: "Attivo",
                table: "Clienti");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Clienti");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Clienti");

            migrationBuilder.DropColumn(
                name: "TimestampSync",
                table: "Clienti");

            migrationBuilder.DropColumn(
                name: "UltimaModifica",
                table: "Clienti");

            migrationBuilder.DropColumn(
                name: "Attivo",
                table: "Articoli");

            migrationBuilder.DropColumn(
                name: "Prezzo",
                table: "Articoli");

            migrationBuilder.DropColumn(
                name: "TimestampSync",
                table: "Articoli");

            migrationBuilder.DropColumn(
                name: "UltimaModifica",
                table: "Articoli");

            migrationBuilder.AlterColumn<int>(
                name: "QuantitaRichiesta",
                table: "Commesse",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ArticoloId",
                table: "Commesse",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clienti_Codice",
                table: "Clienti",
                column: "Codice");

            migrationBuilder.CreateIndex(
                name: "IX_Articoli_Codice",
                table: "Articoli",
                column: "Codice");
        }
    }
}
