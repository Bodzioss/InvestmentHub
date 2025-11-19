using InvestmentHub.Domain.Common;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Events;

/// <summary>
/// Domain event raised when the current market value of an investment is updated.
/// This event tracks changes in investment valuation over time.
/// </summary>
public class InvestmentValueUpdatedEvent : DomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the investment.
    /// </summary>
    public InvestmentId InvestmentId { get; }

    /// <summary>
    /// Gets the portfolio identifier (for cross-stream projections).
    /// </summary>
    public PortfolioId PortfolioId { get; }

    /// <summary>
    /// Gets the previous current value before the update.
    /// </summary>
    public Money OldValue { get; }

    /// <summary>
    /// Gets the new current value after the update.
    /// </summary>
    public Money NewValue { get; }

    /// <summary>
    /// Gets the timestamp when the value was updated.
    /// </summary>
    public DateTime UpdatedAt { get; }

    /// <summary>
    /// Gets the change in value (NewValue - OldValue).
    /// </summary>
    public Money ValueChange { get; }

    /// <summary>
    /// Gets the percentage change in value.
    /// </summary>
    public decimal PercentageChange { get; }

    /// <summary>
    /// Initializes a new instance of the InvestmentValueUpdatedEvent class.
    /// </summary>
    /// <param name="investmentId">The investment identifier</param>
    /// <param name="portfolioId">The portfolio identifier</param>
    /// <param name="oldValue">The previous value</param>
    /// <param name="newValue">The new value</param>
    /// <param name="updatedAt">The update timestamp</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    /// <exception cref="ArgumentException">Thrown when values are invalid</exception>
    public InvestmentValueUpdatedEvent(
        InvestmentId investmentId,
        PortfolioId portfolioId,
        Money oldValue,
        Money newValue,
        DateTime updatedAt) : base(version: 1)
    {
        InvestmentId = investmentId ?? throw new ArgumentNullException(nameof(investmentId));
        PortfolioId = portfolioId ?? throw new ArgumentNullException(nameof(portfolioId));
        OldValue = oldValue ?? throw new ArgumentNullException(nameof(oldValue));
        NewValue = newValue ?? throw new ArgumentNullException(nameof(newValue));

        if (newValue.Amount < 0)
        {
            throw new ArgumentException("New value cannot be negative", nameof(newValue));
        }

        if (oldValue.Currency != newValue.Currency)
        {
            throw new ArgumentException("Currency mismatch between old and new value", nameof(newValue));
        }

        UpdatedAt = updatedAt;

        // Calculate value change
        ValueChange = new Money(newValue.Amount - oldValue.Amount, newValue.Currency);

        // Calculate percentage change
        PercentageChange = oldValue.Amount != 0 
            ? ((newValue.Amount - oldValue.Amount) / oldValue.Amount) * 100 
            : 0;
    }

    /// <summary>
    /// Returns a string representation of the event.
    /// </summary>
    public override string ToString() =>
        $"InvestmentValueUpdated: {InvestmentId.Value} from {OldValue.Amount} to {NewValue.Amount} {NewValue.Currency} ({PercentageChange:F2}%)";
}

