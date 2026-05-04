using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAllegatiNonConformita : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AllegatiNonConformita",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NonConformitaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomeFile = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Dati = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    DimensioneBytes = table.Column<long>(type: "bigint", nullable: false),
                    DataCaricamento = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllegatiNonConformita", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllegatiNonConformita_NonConformita_NonConformitaId",
                        column: x => x.NonConformitaId,
                        principalTable: "NonConformita",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllegatiNonConformita_NonConformitaId",
                table: "AllegatiNonConformita",
                column: "NonConformitaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllegatiNonConformita");
        }
    }
}
