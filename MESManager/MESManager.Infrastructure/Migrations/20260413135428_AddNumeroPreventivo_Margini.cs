using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNumeroPreventivo_Margini : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Margine1",
                table: "Preventivi",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Margine2",
                table: "Preventivi",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Margine3",
                table: "Preventivi",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Margine4",
                table: "Preventivi",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "NumeroPreventivo",
                table: "Preventivi",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Assegna numeri progressivi ai preventivi esistenti, partendo da 1000
            migrationBuilder.Sql(@"
                WITH Ordinati AS (
                    SELECT Id, ROW_NUMBER() OVER (ORDER BY DataCreazione) AS Rn
                    FROM Preventivi
                )
                UPDATE Preventivi
                SET NumeroPreventivo = 999 + Ordinati.Rn
                FROM Ordinati
                WHERE Preventivi.Id = Ordinati.Id;
            ");
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Margine1",
                table: "Preventivi");

            migrationBuilder.DropColumn(
                name: "Margine2",
                table: "Preventivi");

            migrationBuilder.DropColumn(
                name: "Margine3",
                table: "Preventivi");

            migrationBuilder.DropColumn(
                name: "Margine4",
                table: "Preventivi");

            migrationBuilder.DropColumn(
                name: "NumeroPreventivo",
                table: "Preventivi");
        }
    }
}
