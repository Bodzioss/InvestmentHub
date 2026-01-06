using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentHub.Infrastructure.Data.Configurations;

public class InvestmentConfiguration : IEntityTypeConfiguration<Investment>
{
    public void Configure(EntityTypeBuilder<Investment> builder)
    {
        builder.ToTable("Investments");

        builder.HasKey(i => i.Id);

        // PortfolioId value object conversion
        builder.Property(i => i.PortfolioId)
            .HasConversion(
                v => v.Value,
                v => new PortfolioId(v))
            .HasColumnName("PortfolioId");

        // InvestmentId value object conversion  
        builder.Property(i => i.Id)
            .HasConversion(
                v => v.Value,
                v => new InvestmentId(v))
            .HasColumnName("Id");

        // Configure Symbol as owned type with proper column lengths
        builder.OwnsOne(i => i.Symbol, symbol =>
        {
            symbol.Property(s => s.Ticker)
                .HasColumnName("SymbolTicker")
                .HasMaxLength(50)  // Increased from 10 to handle long ETF tickers
                .IsRequired();

            symbol.Property(s => s.Exchange)
                .HasColumnName("SymbolExchange")
                .HasMaxLength(50)
                .IsRequired();

            symbol.Property(s => s.AssetType)
                .HasColumnName("SymbolAssetType")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
        });

        // Configure CurrentValue as owned Money
        builder.OwnsOne(i => i.CurrentValue, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("CurrentValueAmount")
                .HasPrecision(18, 8);

            money.Property(m => m.Currency)
                .HasColumnName("CurrentValueCurrency")
                .HasConversion<string>()
                .HasMaxLength(10);
        });

        // Configure PurchasePrice as owned Money
        builder.OwnsOne(i => i.PurchasePrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("PurchasePriceAmount")
                .HasPrecision(18, 8);

            money.Property(m => m.Currency)
                .HasColumnName("PurchasePriceCurrency")
                .HasConversion<string>()
                .HasMaxLength(10);
        });

        builder.Property(i => i.Quantity)
            .HasPrecision(18, 8);

        builder.Property(i => i.PurchaseDate);
        builder.Property(i => i.LastUpdated);

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}
