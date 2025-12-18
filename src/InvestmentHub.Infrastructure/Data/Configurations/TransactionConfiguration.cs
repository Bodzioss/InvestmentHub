using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentHub.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Transaction entity
/// </summary>
public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        // Transaction ID
        builder.Property(t => t.Id)
            .HasConversion(
                id => id.Value,
                value => new TransactionId(value))
            .HasColumnName("Id")
            .IsRequired();

        // Portfolio ID
        builder.Property(t => t.PortfolioId)
            .HasConversion(
                id => id.Value,
                value => new PortfolioId(value))
            .HasColumnName("PortfolioId")
            .IsRequired();

        // Transaction Type
        builder.Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Symbol - owned entity
        builder.OwnsOne(t => t.Symbol, symbol =>
        {
            symbol.Property(s => s.Ticker)
                .HasColumnName("Symbol")
                .HasMaxLength(20)
                .IsRequired();

            symbol.Property(s => s.Exchange)
                .HasColumnName("Exchange")
                .HasMaxLength(50)
                .IsRequired();

            symbol.Property(s => s.AssetType)
                .HasColumnName("AssetType")
                .HasMaxLength(50)
                .IsRequired();
        });

        // For BUY/SELL transactions
        builder.Property(t => t.Quantity)
            .HasColumnName("Quantity")
            .HasPrecision(18, 8);

        // Price Per Unit - owned Money
        builder.OwnsOne(t => t.PricePerUnit, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("PricePerUnit")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Fee - owned Money (optional)
        builder.OwnsOne(t => t.Fee, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Fee")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("FeeCurrency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        // For DIVIDEND/INTEREST transactions
        // Gross Amount
        builder.OwnsOne(t => t.GrossAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("GrossAmount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("GrossAmountCurrency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        // Tax Rate
        builder.Property(t => t.TaxRate)
            .HasColumnName("TaxRate")
            .HasPrecision(5, 2);

        // Tax Withheld
        builder.OwnsOne(t => t.TaxWithheld, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("TaxWithheld")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("TaxWithheldCurrency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        // Net Amount
        builder.OwnsOne(t => t.NetAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("NetAmount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("NetAmountCurrency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        // For BONDS
        builder.Property(t => t.MaturityDate)
            .HasColumnName("MaturityDate")
            .HasColumnType("date");

        // Transaction Date
        builder.Property(t => t.TransactionDate)
            .HasColumnName("TransactionDate")
            .IsRequired();

        // Status
        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(TransactionStatus.Active)
            .IsRequired();

        // Notes
        builder.Property(t => t.Notes)
            .HasColumnName("Notes")
            .HasColumnType("text");

        // Indexes
        builder.HasIndex(t => t.PortfolioId)
            .HasDatabaseName("idx_transactions_portfolio");

        builder.HasIndex(t => t.TransactionDate)
            .HasDatabaseName("idx_transactions_date");

        builder.HasIndex(t => t.Type)
            .HasDatabaseName("idx_transactions_type");
    }
}
