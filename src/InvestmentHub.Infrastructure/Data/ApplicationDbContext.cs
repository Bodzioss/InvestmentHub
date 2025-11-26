using Microsoft.EntityFrameworkCore;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Infrastructure.Data;

/// <summary>
/// Application database context for InvestmentHub.
/// Handles all database operations for investment and portfolio entities.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Users DbSet.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Gets or sets the Portfolios DbSet.
    /// </summary>
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();

    /// <summary>
    /// Gets or sets the Investments DbSet.
    /// </summary>
    public DbSet<Investment> Investments => Set<Investment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
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
        modelBuilder.Entity<Portfolio>(entity =>
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
        modelBuilder.Entity<Investment>(entity =>
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
                    .HasMaxLength(3)
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
                    .HasMaxLength(3)
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
    }
}

