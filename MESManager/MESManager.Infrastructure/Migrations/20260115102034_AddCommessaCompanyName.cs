using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommessaCompanyName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "Commesse",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "Commesse");
        }
    }
}
