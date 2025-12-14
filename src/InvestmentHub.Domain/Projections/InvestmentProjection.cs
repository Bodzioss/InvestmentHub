using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Events;
using InvestmentHub.Domain.ReadModels;
using Marten.Events.Aggregation;

namespace InvestmentHub.Domain.Projections;

/// <summary>
/// Marten projection that builds InvestmentReadModel from investment events.
/// This is a single-stream projection that processes events for one investment at a time.
/// Registered as an inline projection for immediate consistency.
/// </summary>
public class InvestmentProjection : SingleStreamProjection<InvestmentReadModel>
{
    /// <summary>
    /// Initializes a new instance of the InvestmentProjection class.
    /// </summary>
    public InvestmentProjection()
    {
        // Configure projection identity
        ProjectionName = "Investment";
    }

    /// <summary>
    /// Creates the initial read model from InvestmentAddedEvent.
    /// This is called when a new investment is added.
    /// </summary>
    /// <param name="event">The InvestmentAddedEvent</param>
    /// <returns>A new InvestmentReadModel with initial state</returns>
    public InvestmentReadModel Create(InvestmentAddedEvent @event)
    {
        return new InvestmentReadModel
        {
            Id = @event.InvestmentId.Value,
            PortfolioId = @event.PortfolioId.Value,
            Ticker = @event.Symbol.Ticker,
            Exchange = @event.Symbol.Exchange,
            AssetType = @event.Symbol.AssetType.ToString(),
            PurchasePrice = @event.PurchasePrice.Amount,
            Currency = @event.PurchasePrice.Currency.ToString(),
            Quantity = @event.Quantity,
            OriginalQuantity = @event.Quantity,
            PurchaseDate = @event.PurchaseDate,
            CurrentValue = @event.InitialCurrentValue.Amount,
            Status = InvestmentStatus.Active,
            LastUpdated = @event.OccurredOn,
            SoldDate = null,
            RealizedProfitLoss = null
        };
    }

    /// <summary>
    /// Updates the read model when investment value is updated.
    /// </summary>
    /// <param name="model">The current read model</param>
    /// <param name="event">The InvestmentValueUpdatedEvent</param>
    public static void Apply(InvestmentReadModel model, InvestmentValueUpdatedEvent @event)
    {
        model.CurrentValue = @event.NewValue.Amount;
        model.LastUpdated = @event.UpdatedAt;
    }

    /// <summary>
    /// Updates the read model when investment is sold.
    /// </summary>
    /// <param name="model">The current read model</param>
    /// <param name="event">The InvestmentSoldEvent</param>
    public static void Apply(InvestmentReadModel model, InvestmentSoldEvent @event)
    {
        // Update quantity
        model.Quantity -= @event.QuantitySold;
        
        // Update status
        model.Status = @event.IsCompleteSale ? InvestmentStatus.Sold : InvestmentStatus.PartiallySold;
        
        // Set sold date
        model.SoldDate = @event.SaleDate;
        
        // Set realized profit/loss
        model.RealizedProfitLoss = @event.RealizedProfitLoss.Amount;
        
        // Update current value
        if (@event.IsCompleteSale)
        {
            // All units sold, current value is 0
            model.CurrentValue = 0;
        }
        else
        {
            // Partial sale - recalculate current value for remaining quantity
            // Assuming the value per unit hasn't changed
            var valuePerUnit = model.CurrentValue / (model.Quantity + @event.QuantitySold);
            model.CurrentValue = valuePerUnit * model.Quantity;
        }
        
        model.LastUpdated = @event.OccurredOn;
    }
    /// <summary>
    /// Deletes the read model when investment is deleted.
    /// Returns null to signal Marten to delete the document.
    /// </summary>
    public InvestmentReadModel? Apply(InvestmentReadModel model, InvestmentDeletedEvent @event)
    {
        return null;
    }
}

