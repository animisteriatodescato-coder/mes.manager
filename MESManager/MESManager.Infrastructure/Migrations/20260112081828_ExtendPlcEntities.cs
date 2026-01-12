using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExtendPlcEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ValoreGenerico",
                table: "PLCRealtime",
                newName: "TempoMedioRilevato");

            migrationBuilder.AddColumn<Guid>(
                name: "OperatoreId",
                table: "PLCStorico",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatoMacchina",
                table: "PLCStorico",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BarcodeLavorazione",
                table: "PLCRealtime",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CicliFatti",
                table: "PLCRealtime",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CicliScarti",
                table: "PLCRealtime",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Figure",
                table: "PLCRealtime",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "OperatoreId",
                table: "PLCRealtime",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantitaDaProdurre",
                table: "PLCRealtime",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "QuantitaRaggiunta",
                table: "PLCRealtime",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StatoMacchina",
                table: "PLCRealtime",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TempoMedio",
                table: "PLCRealtime",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NumeroOperatore",
                table: "Operatori",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Dettagli",
                table: "EventiPLC",
                type: "nvarchar(max)",
                nullable: true);

            // Tabella Anime già esistente, skip creazione

            migrationBuilder.CreateIndex(
                name: "IX_PLCStorico_OperatoreId",
                table: "PLCStorico",
                column: "OperatoreId");

            migrationBuilder.CreateIndex(
                name: "IX_PLCRealtime_OperatoreId",
                table: "PLCRealtime",
                column: "OperatoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_PLCRealtime_Operatori_OperatoreId",
                table: "PLCRealtime",
                column: "OperatoreId",
                principalTable: "Operatori",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PLCStorico_Operatori_OperatoreId",
                table: "PLCStorico",
                column: "OperatoreId",
                principalTable: "Operatori",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PLCRealtime_Operatori_OperatoreId",
                table: "PLCRealtime");

            migrationBuilder.DropForeignKey(
                name: "FK_PLCStorico_Operatori_OperatoreId",
                table: "PLCStorico");

            // Tabella Anime già esistente, skip drop

            migrationBuilder.DropIndex(
                name: "IX_PLCStorico_OperatoreId",
                table: "PLCStorico");

            migrationBuilder.DropIndex(
                name: "IX_PLCRealtime_OperatoreId",
                table: "PLCRealtime");

            migrationBuilder.DropColumn(
                name: "OperatoreId",
                table: "PLCStorico");

            migrationBuilder.DropColumn(
                name: "StatoMacchina",
                table: "PLCStorico");

            migrationBuilder.DropColumn(
                name: "BarcodeLavorazione",
                table: "PLCRealtime");

            migrationBuilder.DropColumn(
                name: "CicliFatti",
                table: "PLCRealtime");

            migrationBuilder.DropColumn(
                name: "CicliScarti",
                table: "PLCRealtime");

            migrationBuilder.DropColumn(
                name: "Figure",
                table: "PLCRealtime");

            migrationBuilder.DropColumn(
                name: "OperatoreId",
                table: "PLCRealtime");

            migrationBuilder.DropColumn(
                name: "QuantitaDaProdurre",
                table: "PLCRealtime");

            migrationBuilder.DropColumn(
                name: "QuantitaRaggiunta",
                table: "PLCRealtime");

            migrationBuilder.DropColumn(
                name: "StatoMacchina",
                table: "PLCRealtime");

            migrationBuilder.DropColumn(
                name: "TempoMedio",
                table: "PLCRealtime");

            migrationBuilder.DropColumn(
                name: "NumeroOperatore",
                table: "Operatori");

            migrationBuilder.DropColumn(
                name: "Dettagli",
                table: "EventiPLC");

            migrationBuilder.RenameColumn(
                name: "TempoMedioRilevato",
                table: "PLCRealtime",
                newName: "ValoreGenerico");
        }
    }
}
