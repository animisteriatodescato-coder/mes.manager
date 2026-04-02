using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIndirizzoFtpToMacchina : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IndirizzoFtp",
                table: "Macchine",
                type: "nvarchar(max)",
                nullable: true);

            // Auto-popola IndirizzoFtp derivandolo da IndirizzoPLC: aggiunge 100 all'ultimo ottetto.
            // Esempio: IndirizzoPLC = 192.168.17.26 → IndirizzoFtp = 192.168.17.126
            migrationBuilder.Sql(@"
                UPDATE Macchine
                SET IndirizzoFtp =
                    LEFT(IndirizzoPLC, LEN(IndirizzoPLC) - CHARINDEX('.', REVERSE(IndirizzoPLC)) + 1)
                    + CAST(CAST(RIGHT(IndirizzoPLC, CHARINDEX('.', REVERSE(IndirizzoPLC)) - 1) AS INT) + 100 AS NVARCHAR(5))
                WHERE IndirizzoPLC IS NOT NULL AND IndirizzoPLC <> ''
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IndirizzoFtp",
                table: "Macchine");
        }
    }
}
