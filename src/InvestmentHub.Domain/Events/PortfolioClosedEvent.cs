using InvestmentHub.Domain.Common;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Events;

/// <summary>
/// Domain event raised when a portfolio is closed.
/// This event marks the portfolio as inactive and prevents further operations.
/// </summary>
public class PortfolioClosedEvent : DomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the portfolio.
    /// </summary>
    public PortfolioId PortfolioId { get; }
    
    /// <summary>
    /// Gets the name of the portfolio that was closed.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Gets the reason for closing the portfolio.
    /// </summary>
    public string? Reason { get; }
    
    /// <summary>
    /// Gets the date and time when the portfolio was closed.
    /// </summary>
    public DateTime ClosedAt { get; }
    
    /// <summary>
    /// Gets the unique identifier of the user who closed the portfolio.
    /// </summary>
    public UserId ClosedBy { get; }
    
    /// <summary>
    /// Initializes a new instance of the PortfolioClosedEvent class.
    /// </summary>
    /// <param name="portfolioId">The unique identifier of the portfolio</param>
    /// <param name="name">The name of the portfolio</param>
    /// <param name="reason">Optional reason for closing the portfolio</param>
    /// <param name="closedAt">The date and time when closed</param>
    /// <param name="closedBy">The user who closed the portfolio</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    /// <exception cref="ArgumentException">Thrown when name is empty</exception>
    public PortfolioClosedEvent(
        PortfolioId portfolioId,
        string name,
        string? reason,
        DateTime closedAt,
        UserId closedBy) : base(1)
    {
        PortfolioId = portfolioId ?? throw new ArgumentNullException(nameof(portfolioId));
        ClosedBy = closedBy ?? throw new ArgumentNullException(nameof(closedBy));
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Portfolio name cannot be empty", nameof(name));
        
        Name = name;
        Reason = reason;
        ClosedAt = closedAt;
    }
    
    /// <summary>
    /// Returns a string representation of the PortfolioClosedEvent.
    /// </summary>
    /// <returns>Formatted string with event details</returns>
    public override string ToString()
    {
        var reasonText = string.IsNullOrWhiteSpace(Reason) ? "No reason provided" : $"Reason: {Reason}";
        return $"PortfolioClosed: '{Name}' (ID: {PortfolioId.Value}) at {ClosedAt:yyyy-MM-dd HH:mm:ss}. {reasonText}";
    }
}

