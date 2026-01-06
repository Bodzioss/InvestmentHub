using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEtfDetailsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EtfDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstrumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    YearAdded = table.Column<int>(type: "integer", nullable: true),
                    Region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Theme = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Manager = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DistributionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Domicile = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Replication = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AnnualFeePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    AssetsMillionsEur = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ExtendedTicker = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtfDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EtfDetails_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EtfDetails_InstrumentId",
                table: "EtfDetails",
                column: "InstrumentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EtfDetails");
        }
    }
}
