using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedeStatoLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SchedeStatoLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchedaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoScheda = table.Column<int>(type: "int", nullable: false),
                    StatoPrecedente = table.Column<int>(type: "int", nullable: false),
                    StatoNuovo = table.Column<int>(type: "int", nullable: false),
                    DataCambio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OperatoreId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    NomeOperatore = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchedeStatoLog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SchedeStatoLog_SchedaId_TipoScheda",
                table: "SchedeStatoLog",
                columns: new[] { "SchedaId", "TipoScheda" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SchedeStatoLog");
        }
    }
}
