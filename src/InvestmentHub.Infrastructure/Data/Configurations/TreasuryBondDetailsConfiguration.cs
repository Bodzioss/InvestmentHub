using InvestmentHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentHub.Infrastructure.Data.Configurations;

public class TreasuryBondDetailsConfiguration : IEntityTypeConfiguration<TreasuryBondDetails>
{
    public void Configure(EntityTypeBuilder<TreasuryBondDetails> builder)
    {
        builder.ToTable("TreasuryBondDetails");

        builder.HasKey(b => b.Id);

        // 1:1 relationship with Instrument
        builder.HasOne(b => b.Instrument)
            .WithOne(i => i.BondDetails)
            .HasForeignKey<TreasuryBondDetails>(b => b.InstrumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(b => b.Type)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(b => b.IssueDate)
            .IsRequired();

        builder.Property(b => b.MaturityDate)
            .IsRequired();

        builder.Property(b => b.NominalValue)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(b => b.FirstYearRate)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(b => b.Margin)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(b => b.EarlyRedemptionFee)
            .HasPrecision(18, 2)
            .IsRequired();

        // Index for fast bond type lookups
        builder.HasIndex(b => b.Type);
        builder.HasIndex(b => b.MaturityDate);
    }
}
