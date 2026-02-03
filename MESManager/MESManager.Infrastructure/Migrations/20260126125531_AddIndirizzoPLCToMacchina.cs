using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIndirizzoPLCToMacchina : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: la colonna potrebbe già esistere
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Macchine') AND name = 'IndirizzoPLC')
                BEGIN
                    ALTER TABLE [Macchine] ADD [IndirizzoPLC] nvarchar(max) NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IndirizzoPLC",
                table: "Macchine");
        }
    }
}
