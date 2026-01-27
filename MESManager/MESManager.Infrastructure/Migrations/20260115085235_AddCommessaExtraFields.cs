using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommessaExtraFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Commesse",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalOrdNo",
                table: "Commesse",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternalOrdNo",
                table: "Commesse",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Line",
                table: "Commesse",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OurReference",
                table: "Commesse",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SaleOrdId",
                table: "Commesse",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UoM",
                table: "Commesse",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Commesse");

            migrationBuilder.DropColumn(
                name: "ExternalOrdNo",
                table: "Commesse");

            migrationBuilder.DropColumn(
                name: "InternalOrdNo",
                table: "Commesse");

            migrationBuilder.DropColumn(
                name: "Line",
                table: "Commesse");

            migrationBuilder.DropColumn(
                name: "OurReference",
                table: "Commesse");

            migrationBuilder.DropColumn(
                name: "SaleOrdId",
                table: "Commesse");

            migrationBuilder.DropColumn(
                name: "UoM",
                table: "Commesse");
        }
    }
}
