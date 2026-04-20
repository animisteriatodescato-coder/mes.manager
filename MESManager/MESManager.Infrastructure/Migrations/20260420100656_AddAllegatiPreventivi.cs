using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAllegatiPreventivi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AllegatiPreventivi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PreventivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomeFile = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Dati = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    DimensioneBytes = table.Column<long>(type: "bigint", nullable: false),
                    DataCaricamento = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllegatiPreventivi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllegatiPreventivi_Preventivi_PreventivoId",
                        column: x => x.PreventivoId,
                        principalTable: "Preventivi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllegatiPreventivi_PreventivoId",
                table: "AllegatiPreventivi",
                column: "PreventivoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllegatiPreventivi");
        }
    }
}
