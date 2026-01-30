using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTechnicalIssuesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TechnicalIssues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Environment = table.Column<int>(type: "int", nullable: false),
                    Area = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReproSteps = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Logs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AffectedVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Solution = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RulesLearned = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocsUpdated = table.Column<bool>(type: "bit", nullable: false),
                    DocsReferencePath = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicalIssues", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalIssues_Area",
                table: "TechnicalIssues",
                column: "Area");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalIssues_CreatedAt",
                table: "TechnicalIssues",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalIssues_Environment",
                table: "TechnicalIssues",
                column: "Environment");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalIssues_Severity",
                table: "TechnicalIssues",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalIssues_Status",
                table: "TechnicalIssues",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TechnicalIssues");
        }
    }
}
