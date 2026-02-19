using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBufferInizioProduzioneMinuti : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BufferInizioProduzioneMinuti",
                table: "ImpostazioniGantt",
                type: "int",
                nullable: false,
                defaultValue: 15);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BufferInizioProduzioneMinuti",
                table: "ImpostazioniGantt");
        }
    }
}
