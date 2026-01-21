using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using InvestmentHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Infrastructure.Data;

/// <summary>
/// Application database context for InvestmentHub.
/// Handles all database operations for investment and portfolio entities.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Users DbSet.
    /// Renamed to DomainUsers to avoid conflict with IdentityDbContext.Users.
    /// </summary>
    public DbSet<User> DomainUsers => Set<User>();

    /// <summary>
    /// Gets or sets the Portfolios DbSet.
    /// </summary>
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();

    /// <summary>
    /// Gets or sets the Investments DbSet.
    /// </summary>
    public DbSet<Investment> Investments => Set<Investment>();

    /// <summary>
    /// Gets or sets the Transactions DbSet.
    /// </summary>
    public DbSet<InvestmentHub.Domain.Aggregates.Transaction> Transactions => Set<InvestmentHub.Domain.Aggregates.Transaction>();

    /// <summary>
    /// Gets or sets the FinancialReports DbSet for AI report library.
    /// </summary>
    public DbSet<FinancialReport> FinancialReports => Set<FinancialReport>();

    /// <summary>
    /// Gets or sets the DocumentChunks DbSet for AI vector search.
    /// </summary>
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasPostgresExtension("vector");

        // Configure User entity
        builder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(u => u.Id);

            // Configure UserId as value object
            entity.Property(u => u.Id)
                .HasConversion(
                    id => id.Value,
                    value => new UserId(value))
                .HasColumnName("Id");

            entity.Property(u => u.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(u => u.Email)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(u => u.CreatedAt)
                .IsRequired();

            // Create indexes
            entity.HasIndex(u => u.Email).IsUnique();
        });

        // Configure Portfolio entity
        builder.Entity<Portfolio>(entity =>
        {
            entity.ToTable("Portfolios");

            entity.HasKey(p => p.Id);

            // Configure PortfolioId as value object
            entity.Property(p => p.Id)
                .HasConversion(
                    id => id.Value,
                    value => new PortfolioId(value))
                .HasColumnName("Id");

            // Configure UserId as value object
            entity.Property(p => p.OwnerId)
                .HasConversion(
                    id => id.Value,
                    value => new UserId(value))
                .HasColumnName("OwnerId")
                .IsRequired();

            entity.Property(p => p.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(p => p.Description)
                .HasMaxLength(1000);

            entity.Property(p => p.CreatedDate)
                .IsRequired();

            entity.Property(p => p.LastUpdated)
                .IsRequired();

            entity.Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            // Configure one-to-many relationship with Investments
            entity.HasMany<Investment>()
                .WithOne()
                .HasForeignKey(i => i.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore the domain events collection
            entity.Ignore(p => p.DomainEvents);

            // Ignore the Investments collection as it's managed by EF Core navigation
            entity.Ignore(p => p.Investments);

            // Create index on OwnerId for efficient queries
            entity.HasIndex(p => p.OwnerId);
            entity.HasIndex(p => p.Status);
        });

        // Configure Investment entity
        builder.Entity<Investment>(entity =>
        {
            entity.ToTable("Investments");

            entity.HasKey(i => i.Id);

            // Configure InvestmentId as value object
            entity.Property(i => i.Id)
                .HasConversion(
                    id => id.Value,
                    value => new InvestmentId(value))
                .HasColumnName("Id");

            // Configure PortfolioId as value object
            entity.Property(i => i.PortfolioId)
                .HasConversion(
                    id => id.Value,
                    value => new PortfolioId(value))
                .HasColumnName("PortfolioId")
                .IsRequired();

            // Configure Symbol as owned type (complex value object)
            entity.OwnsOne(i => i.Symbol, symbol =>
            {
                symbol.Property(s => s.Ticker)
                    .HasColumnName("SymbolTicker")
                    .HasMaxLength(50)
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

            // Configure Money value objects
            entity.OwnsOne(i => i.CurrentValue, money =>
            {
                money.Property(m => m.Amount)
                    .HasColumnName("CurrentValueAmount")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                money.Property(m => m.Currency)
                    .HasColumnName("CurrentValueCurrency")
                    .HasConversion<string>()
                    .HasMaxLength(10)
                    .IsRequired();
            });

            entity.OwnsOne(i => i.PurchasePrice, money =>
            {
                money.Property(m => m.Amount)
                    .HasColumnName("PurchasePriceAmount")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                money.Property(m => m.Currency)
                    .HasColumnName("PurchasePriceCurrency")
                    .HasConversion<string>()
                    .HasMaxLength(10)
                    .IsRequired();
            });

            entity.Property(i => i.Quantity)
                .HasColumnType("decimal(18,8)")
                .IsRequired();

            entity.Property(i => i.PurchaseDate)
                .IsRequired();

            entity.Property(i => i.LastUpdated)
                .IsRequired();

            entity.Property(i => i.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            // Create indexes for efficient queries
            entity.HasIndex(i => i.PortfolioId);
            entity.HasIndex(i => i.Status);
            entity.HasIndex(i => new { i.PortfolioId, i.Status });
        });

        // Apply all configurations from assembly
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Configure CachedMarketPrice entity
        builder.Entity<CachedMarketPrice>(entity =>
        {
            entity.ToTable("CachedMarketPrices");

            entity.HasKey(c => c.Id);

            entity.Property(c => c.Symbol)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(c => c.Price)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(c => c.Currency)
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(c => c.FetchedAt)
                .IsRequired();

            entity.Property(c => c.Source)
                .HasMaxLength(50)
                .IsRequired();

            // Create composite index for efficient lookups by symbol and date
            entity.HasIndex(c => new { c.Symbol, c.FetchedAt })
                .HasDatabaseName("IX_CachedMarketPrices_Symbol_FetchedAt");

            // Index on FetchedAt for cleanup queries
            entity.HasIndex(c => c.FetchedAt)
                .HasDatabaseName("IX_CachedMarketPrices_FetchedAt");
        });

        // Configure FinancialReport entity (uses existing Instruments table)
        builder.Entity<FinancialReport>(entity =>
        {
            entity.ToTable("FinancialReports");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Year).IsRequired();
            entity.Property(r => r.ReportType).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(r => r.FileName).HasMaxLength(500).IsRequired();
            entity.Property(r => r.BlobUrl).HasMaxLength(2000).IsRequired();
            entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

            entity.HasOne(r => r.Instrument)
                .WithMany()
                .HasForeignKey(r => r.InstrumentId)
                .OnDelete(DeleteBehavior.Restrict);  // Don't delete reports if instrument is deleted

            entity.HasIndex(r => new { r.InstrumentId, r.Year, r.Quarter, r.ReportType }).IsUnique();
        });

        // Configure DocumentChunk entity with pgvector
        builder.Entity<DocumentChunk>(entity =>
        {
            entity.ToTable("DocumentChunks");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Content).IsRequired();
            entity.Property(c => c.Embedding).HasColumnType("vector(768)");

            entity.HasOne(c => c.Report)
                .WithMany(r => r.Chunks)
                .HasForeignKey(c => c.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => c.ReportId);
        });
    }

    /// <summary>
    /// Gets or sets the Instruments DbSet.
    /// </summary>
    public DbSet<Instrument> Instruments => Set<Instrument>();

    /// <summary>
    /// Gets or sets the CachedMarketPrices DbSet.
    /// </summary>
    public DbSet<CachedMarketPrice> CachedMarketPrices => Set<CachedMarketPrice>();

    /// <summary>
    /// Gets or sets the TreasuryBondDetails DbSet for Polish Treasury Bonds.
    /// </summary>
    public DbSet<TreasuryBondDetails> TreasuryBondDetails => Set<TreasuryBondDetails>();

    /// <summary>
    /// Gets or sets the InterestPeriods DbSet for bond interest tracking.
    /// </summary>
    public DbSet<InterestPeriod> InterestPeriods => Set<InterestPeriod>();
}

