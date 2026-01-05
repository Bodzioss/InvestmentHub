using InvestmentHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentHub.Infrastructure.Data.Configurations;

public class InterestPeriodConfiguration : IEntityTypeConfiguration<InterestPeriod>
{
    public void Configure(EntityTypeBuilder<InterestPeriod> builder)
    {
        builder.ToTable("InterestPeriods");

        builder.HasKey(p => p.Id);

        // Many-to-one relationship with TreasuryBondDetails
        builder.HasOne(p => p.BondDetails)
            .WithMany(b => b.InterestPeriods)
            .HasForeignKey(p => p.BondDetailsId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.PeriodNumber)
            .IsRequired();

        builder.Property(p => p.StartDate)
            .IsRequired();

        builder.Property(p => p.EndDate)
            .IsRequired();

        builder.Property(p => p.InterestRate)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(p => p.AccruedInterest)
            .HasPrecision(18, 4)
            .IsRequired();

        // Composite index for period lookups
        builder.HasIndex(p => new { p.BondDetailsId, p.PeriodNumber }).IsUnique();
        builder.HasIndex(p => new { p.BondDetailsId, p.StartDate, p.EndDate });
    }
}
