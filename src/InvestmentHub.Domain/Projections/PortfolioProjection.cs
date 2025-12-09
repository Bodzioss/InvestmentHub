using InvestmentHub.Domain.Events;
using InvestmentHub.Domain.ReadModels;
using Marten.Events.Aggregation;
using Marten.Events.Projections;

namespace InvestmentHub.Domain.Projections;

/// <summary>
/// Marten projection that builds PortfolioReadModel from portfolio and investment events.
/// This is a multi-stream projection that handles both portfolio-specific events
/// and cross-stream investment events to maintain accurate portfolio totals.
/// </summary>
public class PortfolioProjection : MultiStreamProjection<PortfolioReadModel, Guid>
{
    /// <summary>
    /// Initializes a new instance of the PortfolioProjection class.
    /// </summary>
    public PortfolioProjection()
    {
        ProjectionName = "Portfolio";
        
        // Configure identity for portfolio events - use PortfolioId from event
        Identity<PortfolioCreatedEvent>(e => e.PortfolioId.Value);
        Identity<PortfolioRenamedEvent>(e => e.PortfolioId.Value);
        Identity<PortfolioClosedEvent>(e => e.PortfolioId.Value);
        
        // Configure identity for investment events - use PortfolioId from event
        // These are cross-stream events that update portfolio aggregates
        Identity<InvestmentAddedEvent>(e => e.PortfolioId.Value);
        Identity<InvestmentValueUpdatedEvent>(e => e.PortfolioId.Value);
        Identity<InvestmentSoldEvent>(e => e.PortfolioId.Value);
    }

    /// <summary>
    /// Creates the initial read model from PortfolioCreatedEvent.
    /// This is called when a new portfolio is created.
    /// </summary>
    /// <param name="event">The PortfolioCreatedEvent</param>
    /// <returns>A new PortfolioReadModel with initial state</returns>
    public PortfolioReadModel Create(PortfolioCreatedEvent @event)
    {
        return new PortfolioReadModel
        {
            Id = @event.PortfolioId.Value,
            OwnerId = @event.OwnerId.Value,
            Name = @event.Name,
            Description = @event.Description,
            CreatedAt = @event.CreatedAt,
            IsClosed = false,
            TotalValue = 0,
            Currency = @event.Currency,
            InvestmentCount = 0,
            LastUpdated = @event.OccurredOn,
            AggregateVersion = @event.Version
        };
    }

    /// <summary>
    /// Updates the read model when portfolio is renamed.
    /// </summary>
    /// <param name="model">The current read model</param>
    /// <param name="event">The PortfolioRenamedEvent</param>
    public static void Apply(PortfolioReadModel model, PortfolioRenamedEvent @event)
    {
        model.Name = @event.NewName;
        model.LastUpdated = @event.OccurredOn;
        model.AggregateVersion = @event.Version;
    }

    /// <summary>
    /// Updates the read model when portfolio is closed.
    /// </summary>
    /// <param name="model">The current read model</param>
    /// <param name="event">The PortfolioClosedEvent</param>
    public static void Apply(PortfolioReadModel model, PortfolioClosedEvent @event)
    {
        model.IsClosed = true;
        model.ClosedAt = @event.ClosedAt;
        model.CloseReason = @event.Reason;
        model.LastUpdated = @event.OccurredOn;
        model.AggregateVersion = @event.Version;
    }

    // ==================== Cross-Stream Investment Events ====================

    /// <summary>
    /// Updates the portfolio when an investment is added.
    /// This is a cross-stream projection - the event comes from an Investment stream
    /// but updates the Portfolio read model.
    /// </summary>
    /// <param name="model">The current portfolio read model</param>
    /// <param name="event">The InvestmentAddedEvent</param>
    public static void Apply(PortfolioReadModel model, InvestmentAddedEvent @event)
    {
        // Increment investment count
        model.InvestmentCount++;
        
        // Add the investment value to total portfolio value
        // InitialCurrentValue is the total value (price * quantity)
        model.TotalValue += @event.InitialCurrentValue.Amount;
        
        // Update timestamp
        model.LastUpdated = @event.OccurredOn;
    }

    /// <summary>
    /// Updates the portfolio when an investment's value changes.
    /// This is a cross-stream projection - the event comes from an Investment stream
    /// but updates the Portfolio read model.
    /// </summary>
    /// <param name="model">The current portfolio read model</param>
    /// <param name="event">The InvestmentValueUpdatedEvent</param>
    public static void Apply(PortfolioReadModel model, InvestmentValueUpdatedEvent @event)
    {
        // Update total value by the delta (NewValue - OldValue)
        var valueDelta = @event.NewValue.Amount - @event.OldValue.Amount;
        model.TotalValue += valueDelta;
        
        // Update timestamp
        model.LastUpdated = @event.OccurredOn;
    }

    /// <summary>
    /// Updates the portfolio when an investment is sold.
    /// This is a cross-stream projection - the event comes from an Investment stream
    /// but updates the Portfolio read model.
    /// </summary>
    /// <param name="model">The current portfolio read model</param>
    /// <param name="event">The InvestmentSoldEvent</param>
    public static void Apply(PortfolioReadModel model, InvestmentSoldEvent @event)
    {
        // If complete sale, decrement investment count
        if (@event.IsCompleteSale)
        {
            model.InvestmentCount--;
        }
        
        // NOTE: TotalValue adjustment on sale is complex because we need the current 
        // market value before the sale, which is not included in the InvestmentSoldEvent.
        // 
        // Options for improvement:
        // 1. Add CurrentValueBeforeSale to InvestmentSoldEvent
        // 2. Make TotalValue a computed property (sum of all InvestmentReadModel.CurrentValue)
        // 3. Use TotalProceeds as approximation (assumes sale at market value)
        //
        // For now, we'll use TotalProceeds as approximation:
        model.TotalValue -= @event.TotalProceeds.Amount;
        
        // Update timestamp
        model.LastUpdated = @event.OccurredOn;
    }
}
