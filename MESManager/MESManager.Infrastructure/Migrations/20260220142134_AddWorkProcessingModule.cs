using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkProcessingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WorkProcessingTypeId",
                table: "QuoteRows",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkProcessingTechnicalData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuoteRowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PesoKg = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Figure = table.Column<int>(type: "int", nullable: false),
                    Lotto = table.Column<int>(type: "int", nullable: false),
                    SpariOrari = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    VerniciaturaPezziOra = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IncollaggioOre = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ImballaggioOre = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    VernicePesoKg = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CostoAnimaCalcolato = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CostoFuoriMacchina = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CostoTotalePezzo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MargineApplicatoPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PrezzoVenditaPezzo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkProcessingTechnicalData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkProcessingTechnicalData_QuoteRows_QuoteRowId",
                        column: x => x.QuoteRowId,
                        principalTable: "QuoteRows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkProcessingTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Codice = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Categoria = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Ordinamento = table.Column<int>(type: "int", nullable: false),
                    Attivo = table.Column<bool>(type: "bit", nullable: false),
                    Archiviato = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkProcessingTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkProcessingParameters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkProcessingTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EuroOra = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SabbiaCostoKg = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CostoAttrezzatura = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    VerniceCostoPezzo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    VerniciaturaCostoOra = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IncollaggioCostoOra = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ImballaggioOra = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MargineDefaultPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    VersionNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkProcessingParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkProcessingParameters_WorkProcessingTypes_WorkProcessingTypeId",
                        column: x => x.WorkProcessingTypeId,
                        principalTable: "WorkProcessingTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteRows_WorkProcessingTypeId",
                table: "QuoteRows",
                column: "WorkProcessingTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkProcessingParameters_ValidFrom",
                table: "WorkProcessingParameters",
                column: "ValidFrom");

            migrationBuilder.CreateIndex(
                name: "IX_WorkProcessingParameters_ValidTo",
                table: "WorkProcessingParameters",
                column: "ValidTo");

            migrationBuilder.CreateIndex(
                name: "IX_WorkProcessingParameters_WorkProcessingTypeId_IsCurrent",
                table: "WorkProcessingParameters",
                columns: new[] { "WorkProcessingTypeId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkProcessingTechnicalData_QuoteRowId",
                table: "WorkProcessingTechnicalData",
                column: "QuoteRowId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkProcessingTypes_Attivo",
                table: "WorkProcessingTypes",
                column: "Attivo");

            migrationBuilder.CreateIndex(
                name: "IX_WorkProcessingTypes_Categoria",
                table: "WorkProcessingTypes",
                column: "Categoria");

            migrationBuilder.CreateIndex(
                name: "IX_WorkProcessingTypes_Codice",
                table: "WorkProcessingTypes",
                column: "Codice",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_QuoteRows_WorkProcessingTypes_WorkProcessingTypeId",
                table: "QuoteRows",
                column: "WorkProcessingTypeId",
                principalTable: "WorkProcessingTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuoteRows_WorkProcessingTypes_WorkProcessingTypeId",
                table: "QuoteRows");

            migrationBuilder.DropTable(
                name: "WorkProcessingParameters");

            migrationBuilder.DropTable(
                name: "WorkProcessingTechnicalData");

            migrationBuilder.DropTable(
                name: "WorkProcessingTypes");

            migrationBuilder.DropIndex(
                name: "IX_QuoteRows_WorkProcessingTypeId",
                table: "QuoteRows");

            migrationBuilder.DropColumn(
                name: "WorkProcessingTypeId",
                table: "QuoteRows");
        }
    }
}
