using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlcServiceStatusAndLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlcServiceStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsRunning = table.Column<bool>(type: "bit", nullable: false),
                    ServiceStartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastHeartbeat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServiceVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PollingIntervalSeconds = table.Column<int>(type: "int", nullable: false),
                    EnableRealtime = table.Column<bool>(type: "bit", nullable: false),
                    EnableStorico = table.Column<bool>(type: "bit", nullable: false),
                    EnableEvents = table.Column<bool>(type: "bit", nullable: false),
                    TotalSyncCount = table.Column<int>(type: "int", nullable: false),
                    TotalErrorCount = table.Column<int>(type: "int", nullable: false),
                    LastSyncTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastErrorTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MachinesConfigured = table.Column<int>(type: "int", nullable: false),
                    MachinesConnected = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlcServiceStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlcSyncLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MacchinaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MacchinaNumero = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Level = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlcSyncLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlcSyncLogs_Macchine_MacchinaId",
                        column: x => x.MacchinaId,
                        principalTable: "Macchine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlcSyncLogs_Level",
                table: "PlcSyncLogs",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_PlcSyncLogs_MacchinaId",
                table: "PlcSyncLogs",
                column: "MacchinaId");

            migrationBuilder.CreateIndex(
                name: "IX_PlcSyncLogs_Timestamp",
                table: "PlcSyncLogs",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlcServiceStatus");

            migrationBuilder.DropTable(
                name: "PlcSyncLogs");
        }
    }
}
