using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnimeTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataUltimaModificaLocale",
                table: "Anime",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ModificatoLocalmente",
                table: "Anime",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UtenteUltimaModificaLocale",
                table: "Anime",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataUltimaModificaLocale",
                table: "Anime");

            migrationBuilder.DropColumn(
                name: "ModificatoLocalmente",
                table: "Anime");

            migrationBuilder.DropColumn(
                name: "UtenteUltimaModificaLocale",
                table: "Anime");
        }
    }
}
