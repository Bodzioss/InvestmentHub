using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCachedMarketPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CachedMarketPrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedMarketPrices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CachedMarketPrices_FetchedAt",
                table: "CachedMarketPrices",
                column: "FetchedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CachedMarketPrices_Symbol_FetchedAt",
                table: "CachedMarketPrices",
                columns: new[] { "Symbol", "FetchedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CachedMarketPrices");
        }
    }
}
