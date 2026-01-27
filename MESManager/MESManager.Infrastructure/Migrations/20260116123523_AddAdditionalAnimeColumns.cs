using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdditionalAnimeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArmataL",
                table: "Anime",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Assemblata",
                table: "Anime",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cliente",
                table: "Anime",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Figure",
                table: "Anime",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Incollata",
                table: "Anime",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Maschere",
                table: "Anime",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumeroPiani",
                table: "Anime",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Piastra",
                table: "Anime",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantitaPiano",
                table: "Anime",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TogliereSparo",
                table: "Anime",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArmataL",
                table: "Anime");

            migrationBuilder.DropColumn(
                name: "Assemblata",
                table: "Anime");

            migrationBuilder.DropColumn(
                name: "Cliente",
                table: "Anime");

            migrationBuilder.DropColumn(
                name: "Figure",
                table: "Anime");

            migrationBuilder.DropColumn(
                name: "Incollata",
                table: "Anime");

            migrationBuilder.DropColumn(
                name: "Maschere",
                table: "Anime");

            migrationBuilder.DropColumn(
                name: "NumeroPiani",
                table: "Anime");

            migrationBuilder.DropColumn(
                name: "Piastra",
                table: "Anime");

            migrationBuilder.DropColumn(
                name: "QuantitaPiano",
                table: "Anime");

            migrationBuilder.DropColumn(
                name: "TogliereSparo",
                table: "Anime");
        }
    }
}
