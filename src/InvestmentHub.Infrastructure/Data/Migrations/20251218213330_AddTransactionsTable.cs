using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PortfolioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Exchange = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AssetType = table.Column<int>(type: "integer", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    PricePerUnit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<int>(type: "integer", maxLength: 3, nullable: true),
                    Fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    FeeCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    GrossAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    GrossAmountCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    TaxRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    TaxWithheld = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    TaxWithheldCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    NetAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    NetAmountCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    MaturityDate = table.Column<DateTime>(type: "date", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_transactions_date",
                table: "Transactions",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "idx_transactions_portfolio",
                table: "Transactions",
                column: "PortfolioId");

            migrationBuilder.CreateIndex(
                name: "idx_transactions_type",
                table: "Transactions",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transactions");
        }
    }
}
