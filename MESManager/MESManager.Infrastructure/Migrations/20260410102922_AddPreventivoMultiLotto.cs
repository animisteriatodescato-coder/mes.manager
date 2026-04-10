using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPreventivoMultiLotto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "EuroOraVerniciatura",
                table: "Preventivi",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "EuroOraIncollaggio",
                table: "Preventivi",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "EuroOraImballaggio",
                table: "Preventivi",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<int>(
                name: "Lotto2",
                table: "Preventivi",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Lotto3",
                table: "Preventivi",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Lotto4",
                table: "Preventivi",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Lotto2",
                table: "Preventivi");

            migrationBuilder.DropColumn(
                name: "Lotto3",
                table: "Preventivi");

            migrationBuilder.DropColumn(
                name: "Lotto4",
                table: "Preventivi");

            migrationBuilder.AlterColumn<decimal>(
                name: "EuroOraVerniciatura",
                table: "Preventivi",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "EuroOraIncollaggio",
                table: "Preventivi",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "EuroOraImballaggio",
                table: "Preventivi",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldScale: 2);
        }
    }
}
