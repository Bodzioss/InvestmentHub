using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.ReadModels;

/// <summary>
/// Read model for Portfolio queries using Marten projections.
/// This is a denormalized view optimized for fast reads.
/// Updated automatically from portfolio events.
/// </summary>
public class PortfolioReadModel
{
    /// <summary>
    /// Gets or sets the unique identifier of the portfolio.
    /// This is the primary key for Marten document storage.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Gets or sets the unique identifier of the portfolio owner.
    /// </summary>
    public Guid OwnerId { get; set; }
    
    /// <summary>
    /// Gets or sets the portfolio name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the optional portfolio description.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the portfolio was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the portfolio is closed.
    /// </summary>
    public bool IsClosed { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the portfolio was closed.
    /// </summary>
    public DateTime? ClosedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the reason for closing the portfolio.
    /// </summary>
    public string? CloseReason { get; set; }
    
    /// <summary>
    /// Gets or sets the total value of all investments in the portfolio.
    /// This is calculated from investment events.
    /// </summary>
    public decimal TotalValue { get; set; }
    
    /// <summary>
    /// Gets or sets the currency of the total value.
    /// Defaults to USD for now (multi-currency support can be added later).
    /// </summary>
    public string Currency { get; set; } = "USD";
    
    /// <summary>
    /// Gets or sets the number of active investments in the portfolio.
    /// </summary>
    public int InvestmentCount { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the read model was last updated.
    /// Useful for tracking projection freshness.
    /// </summary>
    public DateTime LastUpdated { get; set; }
    
    /// <summary>
    /// Gets or sets the version of the aggregate.
    /// Used for optimistic concurrency and tracking event replay progress.
    /// </summary>
    public int Version { get; set; }
}

