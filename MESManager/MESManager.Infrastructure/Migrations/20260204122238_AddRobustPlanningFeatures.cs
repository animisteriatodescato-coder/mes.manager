using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRobustPlanningFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Bloccata",
                table: "Commesse",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ClasseLavorazione",
                table: "Commesse",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priorita",
                table: "Commesse",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Commesse",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "SetupStimatoMinuti",
                table: "Commesse",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VincoloDataFine",
                table: "Commesse",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VincoloDataInizio",
                table: "Commesse",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClasseLavorazione",
                table: "Articoli",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bloccata",
                table: "Commesse");

            migrationBuilder.DropColumn(
                name: "ClasseLavorazione",
                table: "Commesse");

            migrationBuilder.DropColumn(
                name: "Priorita",
                table: "Commesse");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Commesse");

            migrationBuilder.DropColumn(
                name: "SetupStimatoMinuti",
                table: "Commesse");

            migrationBuilder.DropColumn(
                name: "VincoloDataFine",
                table: "Commesse");

            migrationBuilder.DropColumn(
                name: "VincoloDataInizio",
                table: "Commesse");

            migrationBuilder.DropColumn(
                name: "ClasseLavorazione",
                table: "Articoli");
        }
    }
}
