using InvestmentHub.Domain.Events;
using InvestmentHub.Domain.ReadModels;
using Marten.Events.Aggregation;

namespace InvestmentHub.Domain.Projections;

/// <summary>
/// Marten projection that builds PortfolioReadModel from portfolio events.
/// This is a single-stream projection that processes events for one portfolio at a time.
/// </summary>
public class PortfolioProjection : SingleStreamProjection<PortfolioReadModel>
{
    /// <summary>
    /// Initializes a new instance of the PortfolioProjection class.
    /// </summary>
    public PortfolioProjection()
    {
        // Configure projection identity - use PortfolioId from events as document Id
        ProjectionName = "Portfolio";
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
            Currency = "USD",
            InvestmentCount = 0,
            LastUpdated = @event.OccurredOn,
            Version = @event.Version
        };
    }

    /// <summary>
    /// Updates the read model when portfolio is renamed.
    /// </summary>
    /// <param name="model">The current read model</param>
    /// <param name="event">The PortfolioRenamedEvent</param>
    public void Apply(PortfolioReadModel model, PortfolioRenamedEvent @event)
    {
        model.Name = @event.NewName;
        model.LastUpdated = @event.OccurredOn;
        model.Version = @event.Version;
    }

    /// <summary>
    /// Updates the read model when portfolio is closed.
    /// </summary>
    /// <param name="model">The current read model</param>
    /// <param name="event">The PortfolioClosedEvent</param>
    public void Apply(PortfolioReadModel model, PortfolioClosedEvent @event)
    {
        model.IsClosed = true;
        model.ClosedAt = @event.ClosedAt;
        model.CloseReason = @event.Reason;
        model.LastUpdated = @event.OccurredOn;
        model.Version = @event.Version;
    }

    // Note: InvestmentAdded/Removed events can be handled here to update TotalValue
    // This will be implemented in later milestones when we add investment events to portfolio stream
}

