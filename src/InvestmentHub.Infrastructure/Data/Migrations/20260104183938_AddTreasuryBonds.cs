using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTreasuryBonds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTE: FinancialReports and DocumentChunks tables already exist from AddAIFinancialReports.sql
            // This migration only adds TreasuryBondDetails and InterestPeriods tables

            migrationBuilder.CreateTable(
                name: "TreasuryBondDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstrumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaturityDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NominalValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FirstYearRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Margin = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    EarlyRedemptionFee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreasuryBondDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreasuryBondDetails_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InterestPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BondDetailsId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodNumber = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InterestRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    AccruedInterest = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterestPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterestPeriods_TreasuryBondDetails_BondDetailsId",
                        column: x => x.BondDetailsId,
                        principalTable: "TreasuryBondDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterestPeriods_BondDetailsId_PeriodNumber",
                table: "InterestPeriods",
                columns: new[] { "BondDetailsId", "PeriodNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterestPeriods_BondDetailsId_StartDate_EndDate",
                table: "InterestPeriods",
                columns: new[] { "BondDetailsId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TreasuryBondDetails_InstrumentId",
                table: "TreasuryBondDetails",
                column: "InstrumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TreasuryBondDetails_MaturityDate",
                table: "TreasuryBondDetails",
                column: "MaturityDate");

            migrationBuilder.CreateIndex(
                name: "IX_TreasuryBondDetails_Type",
                table: "TreasuryBondDetails",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterestPeriods");

            migrationBuilder.DropTable(
                name: "TreasuryBondDetails");
        }
    }
}
