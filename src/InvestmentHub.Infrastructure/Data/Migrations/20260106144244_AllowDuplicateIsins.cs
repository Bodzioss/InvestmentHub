using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllowDuplicateIsins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Instruments_Isin",
                table: "Instruments");

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_Isin",
                table: "Instruments",
                column: "Isin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Instruments_Isin",
                table: "Instruments");

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_Isin",
                table: "Instruments",
                column: "Isin",
                unique: true);
        }
    }
}
