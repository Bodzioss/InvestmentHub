using InvestmentHub.Domain.Common;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Events;

/// <summary>
/// Domain event raised when a portfolio's name is changed.
/// This event captures the name change and maintains the audit trail.
/// </summary>
public class PortfolioRenamedEvent : DomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the portfolio.
    /// </summary>
    public PortfolioId PortfolioId { get; }
    
    /// <summary>
    /// Gets the previous name of the portfolio.
    /// </summary>
    public string OldName { get; }
    
    /// <summary>
    /// Gets the new name of the portfolio.
    /// </summary>
    public string NewName { get; }
    
    /// <summary>
    /// Gets the date and time when the portfolio was renamed.
    /// </summary>
    public DateTime RenamedAt { get; }
    
    /// <summary>
    /// Initializes a new instance of the PortfolioRenamedEvent class.
    /// </summary>
    /// <param name="portfolioId">The unique identifier of the portfolio</param>
    /// <param name="oldName">The previous name of the portfolio</param>
    /// <param name="newName">The new name of the portfolio</param>
    /// <param name="renamedAt">The date and time of the rename</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    /// <exception cref="ArgumentException">Thrown when names are invalid or identical</exception>
    public PortfolioRenamedEvent(
        PortfolioId portfolioId,
        string oldName,
        string newName,
        DateTime renamedAt) : base(1)
    {
        PortfolioId = portfolioId ?? throw new ArgumentNullException(nameof(portfolioId));
        
        if (string.IsNullOrWhiteSpace(oldName))
            throw new ArgumentException("Old name cannot be empty", nameof(oldName));
        
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("New name cannot be empty", nameof(newName));
        
        if (oldName.Equals(newName, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("New name must be different from old name", nameof(newName));
        
        OldName = oldName;
        NewName = newName;
        RenamedAt = renamedAt;
    }
    
    /// <summary>
    /// Returns a string representation of the PortfolioRenamedEvent.
    /// </summary>
    /// <returns>Formatted string with event details</returns>
    public override string ToString() => 
        $"PortfolioRenamed: '{OldName}' → '{NewName}' (ID: {PortfolioId.Value}) at {RenamedAt:yyyy-MM-dd HH:mm:ss}";
}

