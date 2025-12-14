using InvestmentHub.Domain.Common;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Events;

/// <summary>
/// Event raised when an investment is deleted/removed from a portfolio.
/// </summary>
public class InvestmentDeletedEvent : DomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the investment.
    /// </summary>
    public InvestmentId InvestmentId { get; }

    /// <summary>
    /// Gets the unique identifier of the portfolio.
    /// </summary>
    public PortfolioId PortfolioId { get; }

    /// <summary>
    /// Gets the reason for deletion.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Initializes a new instance of the InvestmentDeletedEvent class.
    /// </summary>
    public InvestmentDeletedEvent(
        InvestmentId investmentId,
        PortfolioId portfolioId,
        string reason,
        DateTime occurredOn) : base(version: 1)
    {
        InvestmentId = investmentId ?? throw new ArgumentNullException(nameof(investmentId));
        PortfolioId = portfolioId ?? throw new ArgumentNullException(nameof(portfolioId));
        Reason = reason ?? string.Empty;
        // OccurredOn is managed by base DomainEvent, typically set to UtcNow on init.
        // If we want to override it or set it from parameter, DomainEvent might need a constructor for it.
        // Looking at DomainEvent.cs, it sets OccurredOn = DateTime.UtcNow in constructor.
        // If we need to pass a specific time, we might need to modify base or ignore it if close enough.
        // But usually Event creation time IS OccurredOn.
    }
}
