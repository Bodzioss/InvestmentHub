using InvestmentHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentHub.Infrastructure.Data.Configurations;

public class EtfDetailsConfiguration : IEntityTypeConfiguration<EtfDetails>
{
    public void Configure(EntityTypeBuilder<EtfDetails> builder)
    {
        builder.ToTable("EtfDetails");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.InstrumentId)
            .IsRequired();

        builder.Property(e => e.YearAdded);

        builder.Property(e => e.Region)
            .HasMaxLength(100);

        builder.Property(e => e.Theme)
            .HasMaxLength(100);

        builder.Property(e => e.Manager)
            .HasMaxLength(100);

        builder.Property(e => e.DistributionType)
            .HasMaxLength(50);

        builder.Property(e => e.Domicile)
            .HasMaxLength(100);

        builder.Property(e => e.Replication)
            .HasMaxLength(50);

        builder.Property(e => e.AnnualFeePercent)
            .HasPrecision(5, 2);

        builder.Property(e => e.AssetsMillionsEur)
            .HasPrecision(18, 2);

        builder.Property(e => e.Currency)
            .HasMaxLength(10);

        builder.Property(e => e.ExtendedTicker)
            .HasMaxLength(50);

        // One-to-one relationship with Instrument
        builder.HasOne(e => e.Instrument)
            .WithOne(i => i.EtfDetails)
            .HasForeignKey<EtfDetails>(e => e.InstrumentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index on InstrumentId
        builder.HasIndex(e => e.InstrumentId)
            .IsUnique();
    }
}
