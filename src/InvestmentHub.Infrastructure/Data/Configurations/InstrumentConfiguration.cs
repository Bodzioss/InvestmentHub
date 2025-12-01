using InvestmentHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentHub.Infrastructure.Data.Configurations;

public class InstrumentConfiguration : IEntityTypeConfiguration<Instrument>
{
    public void Configure(EntityTypeBuilder<Instrument> builder)
    {
        builder.ToTable("Instruments");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(i => i.Isin)
            .HasMaxLength(12)
            .IsRequired();

        // Configure Symbol as owned type
        builder.OwnsOne(i => i.Symbol, symbol =>
        {
            symbol.Property(s => s.Ticker)
                .HasColumnName("SymbolTicker")
                .HasMaxLength(10)
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
            
            // Create index on Ticker for fast lookups
            symbol.HasIndex(s => s.Ticker);
        });

        // Create index on ISIN
        builder.HasIndex(i => i.Isin).IsUnique();
    }
}
