using InvestmentHub.Domain.Common;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Events;

/// <summary>
/// Domain event raised when a new portfolio is created.
/// This is the first event in a portfolio's event stream.
/// </summary>
public class PortfolioCreatedEvent : DomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the portfolio.
    /// </summary>
    public PortfolioId PortfolioId { get; }
    
    /// <summary>
    /// Gets the unique identifier of the portfolio owner.
    /// </summary>
    public UserId OwnerId { get; }
    
    /// <summary>
    /// Gets the name of the portfolio.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Gets the optional description of the portfolio.
    /// </summary>
    public string? Description { get; }
    
    /// <summary>
    /// Gets the date and time when the portfolio was created.
    /// </summary>
    public DateTime CreatedAt { get; }
    
    /// <summary>
    /// Gets the currency of the portfolio.
    /// </summary>
    public string Currency { get; }

    /// <summary>
    /// Initializes a new instance of the PortfolioCreatedEvent class.
    /// </summary>
    /// <param name="portfolioId">The unique identifier of the portfolio</param>
    /// <param name="ownerId">The unique identifier of the portfolio owner</param>
    /// <param name="name">The name of the portfolio</param>
    /// <param name="description">Optional description of the portfolio</param>
    /// <param name="currency">The portfolio currency</param>
    /// <param name="createdAt">The creation date and time</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    /// <exception cref="ArgumentException">Thrown when name is empty or whitespace</exception>
    public PortfolioCreatedEvent(
        PortfolioId portfolioId,
        UserId ownerId,
        string name,
        string? description,
        string currency,
        DateTime createdAt) : base(1)
    {
        PortfolioId = portfolioId ?? throw new ArgumentNullException(nameof(portfolioId));
        OwnerId = ownerId ?? throw new ArgumentNullException(nameof(ownerId));
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Portfolio name cannot be empty", nameof(name));
        
        Name = name;
        Description = description;
        Currency = currency;
        CreatedAt = createdAt;
    }
    
    /// <summary>
    /// Returns a string representation of the PortfolioCreatedEvent.
    /// </summary>
    /// <returns>Formatted string with event details</returns>
    public override string ToString() => 
        $"PortfolioCreated: '{Name}' (ID: {PortfolioId.Value}) for Owner {OwnerId.Value} at {CreatedAt:yyyy-MM-dd HH:mm:ss}";
}

