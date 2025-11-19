using InvestmentHub.Domain.Common;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Events;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Aggregates;

/// <summary>
/// Aggregate root for Investment using Event Sourcing pattern.
/// Manages investment state through events and provides business logic for investment lifecycle.
/// </summary>
public class InvestmentAggregate : AggregateRoot
{
    // State - rebuilt from events
    /// <summary>Gets the unique identifier of the investment (also serves as stream ID).</summary>
    public Guid Id { get; private set; }
    
    /// <summary>Gets the investment identifier value object.</summary>
    public InvestmentId InvestmentId { get; private set; } = null!;
    
    /// <summary>Gets the portfolio identifier that owns this investment.</summary>
    public PortfolioId PortfolioId { get; private set; } = null!;
    
    /// <summary>Gets the investment symbol (ticker, exchange, asset type).</summary>
    public Symbol Symbol { get; private set; } = null!;
    
    /// <summary>Gets the current market value of the investment.</summary>
    public Money CurrentValue { get; private set; } = null!;
    
    /// <summary>Gets the original purchase price per unit.</summary>
    public Money PurchasePrice { get; private set; } = null!;
    
    /// <summary>Gets the quantity of units currently held.</summary>
    public decimal Quantity { get; private set; }
    
    /// <summary>Gets the original quantity purchased (for tracking partial sales).</summary>
    public decimal OriginalQuantity { get; private set; }
    
    /// <summary>Gets the date when the investment was purchased.</summary>
    public DateTime PurchaseDate { get; private set; }
    
    /// <summary>Gets the current status of the investment.</summary>
    public InvestmentStatus Status { get; private set; }
    
    /// <summary>Gets the date when the investment was last updated.</summary>
    public DateTime LastUpdated { get; private set; }
    
    /// <summary>Gets the date when the investment was sold (if applicable).</summary>
    public DateTime? SoldDate { get; private set; }
    
    /// <summary>Gets the realized profit/loss if sold (null if still active).</summary>
    public Money? RealizedProfitLoss { get; private set; }

    /// <summary>
    /// Parameterless constructor for Marten event stream replay.
    /// </summary>
    public InvestmentAggregate()
    {
    }

    // Business methods - generate events

    /// <summary>
    /// Creates a new investment aggregate.
    /// This is a factory method that generates the initial InvestmentAddedEvent.
    /// </summary>
    /// <param name="investmentId">The unique identifier for the investment</param>
    /// <param name="portfolioId">The portfolio identifier</param>
    /// <param name="symbol">The investment symbol</param>
    /// <param name="purchasePrice">The purchase price per unit</param>
    /// <param name="quantity">The quantity purchased</param>
    /// <param name="purchaseDate">The purchase date</param>
    /// <returns>A new InvestmentAggregate instance with InvestmentAddedEvent</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    /// <exception cref="ArgumentException">Thrown when values are invalid</exception>
    public static InvestmentAggregate Create(
        InvestmentId investmentId,
        PortfolioId portfolioId,
        Symbol symbol,
        Money purchasePrice,
        decimal quantity,
        DateTime purchaseDate)
    {
        if (investmentId == null)
            throw new ArgumentNullException(nameof(investmentId));
        if (portfolioId == null)
            throw new ArgumentNullException(nameof(portfolioId));
        if (symbol == null)
            throw new ArgumentNullException(nameof(symbol));
        if (purchasePrice == null)
            throw new ArgumentNullException(nameof(purchasePrice));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        if (purchasePrice.Amount <= 0)
            throw new ArgumentException("Purchase price must be greater than zero", nameof(purchasePrice));
        if (purchaseDate > DateTime.UtcNow)
            throw new ArgumentException("Purchase date cannot be in the future", nameof(purchaseDate));

        var aggregate = new InvestmentAggregate();
        
        // Initial current value equals purchase price
        var initialCurrentValue = new Money(purchasePrice.Amount * quantity, purchasePrice.Currency);
        
        var @event = new InvestmentAddedEvent(
            portfolioId,
            investmentId,
            symbol,
            purchasePrice,
            quantity,
            purchaseDate,
            initialCurrentValue);

        aggregate.Apply(@event);
        aggregate.AddDomainEvent(@event);
        
        return aggregate;
    }

    /// <summary>
    /// Updates the current market value of the investment.
    /// Generates an InvestmentValueUpdatedEvent if the value has changed.
    /// </summary>
    /// <param name="newValuePerUnit">The new value per unit</param>
    /// <exception cref="InvalidOperationException">Thrown when investment is sold</exception>
    /// <exception cref="ArgumentNullException">Thrown when newValuePerUnit is null</exception>
    /// <exception cref="ArgumentException">Thrown when value is invalid or currency mismatch</exception>
    public void UpdateValue(Money newValuePerUnit)
    {
        if (Status == InvestmentStatus.Sold)
            throw new InvalidOperationException("Cannot update value of a sold investment");

        if (newValuePerUnit == null)
            throw new ArgumentNullException(nameof(newValuePerUnit));

        if (newValuePerUnit.Amount < 0)
            throw new ArgumentException("Value cannot be negative", nameof(newValuePerUnit));

        if (newValuePerUnit.Currency != CurrentValue.Currency)
            throw new ArgumentException("Currency mismatch with current value", nameof(newValuePerUnit));

        // Calculate total new value
        var newTotalValue = new Money(newValuePerUnit.Amount * Quantity, newValuePerUnit.Currency);
        
        // Only generate event if value actually changed
        if (Math.Abs(newTotalValue.Amount - CurrentValue.Amount) < 0.01m)
            return; // No significant change

        var @event = new InvestmentValueUpdatedEvent(
            InvestmentId,
            PortfolioId,
            CurrentValue,
            newTotalValue,
            DateTime.UtcNow);

        Apply(@event);
        AddDomainEvent(@event);
    }

    /// <summary>
    /// Sells the investment (fully or partially).
    /// Generates an InvestmentSoldEvent.
    /// </summary>
    /// <param name="salePrice">The sale price per unit</param>
    /// <param name="quantityToSell">The quantity to sell (null = sell all)</param>
    /// <param name="saleDate">The sale date</param>
    /// <exception cref="InvalidOperationException">Thrown when investment is already sold or quantity invalid</exception>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    /// <exception cref="ArgumentException">Thrown when values are invalid</exception>
    public void Sell(Money salePrice, decimal? quantityToSell, DateTime saleDate)
    {
        if (Status == InvestmentStatus.Sold)
            throw new InvalidOperationException("Investment is already sold");

        if (salePrice == null)
            throw new ArgumentNullException(nameof(salePrice));

        if (salePrice.Amount < 0)
            throw new ArgumentException("Sale price cannot be negative", nameof(salePrice));

        if (salePrice.Currency != PurchasePrice.Currency)
            throw new ArgumentException("Currency mismatch with purchase price", nameof(salePrice));

        if (saleDate > DateTime.UtcNow)
            throw new ArgumentException("Sale date cannot be in the future", nameof(saleDate));

        if (saleDate < PurchaseDate)
            throw new ArgumentException("Sale date cannot be before purchase date", nameof(saleDate));

        // Determine quantity to sell
        var actualQuantityToSell = quantityToSell ?? Quantity;

        if (actualQuantityToSell <= 0)
            throw new ArgumentException("Quantity to sell must be greater than zero", nameof(quantityToSell));

        if (actualQuantityToSell > Quantity)
            throw new ArgumentException($"Cannot sell {actualQuantityToSell} units - only {Quantity} available", nameof(quantityToSell));

        // Determine if this is a complete sale
        bool isCompleteSale = Math.Abs(actualQuantityToSell - Quantity) < 0.0001m;

        var @event = new InvestmentSoldEvent(
            InvestmentId,
            PortfolioId,
            salePrice,
            actualQuantityToSell,
            saleDate,
            PurchasePrice,
            isCompleteSale);

        Apply(@event);
        AddDomainEvent(@event);
    }

    // Apply methods - update state from events (Event Sourcing pattern)

    /// <summary>
    /// Applies an InvestmentAddedEvent to set the initial state.
    /// This method is called during event stream replay.
    /// </summary>
    /// <param name="event">The InvestmentAddedEvent to apply</param>
    public void Apply(InvestmentAddedEvent @event)
    {
        Id = @event.InvestmentId.Value;
        InvestmentId = @event.InvestmentId;
        PortfolioId = @event.PortfolioId;
        Symbol = @event.Symbol;
        PurchasePrice = @event.PurchasePrice;
        Quantity = @event.Quantity;
        OriginalQuantity = @event.Quantity;
        PurchaseDate = @event.PurchaseDate;
        CurrentValue = @event.InitialCurrentValue;
        Status = InvestmentStatus.Active;
        LastUpdated = @event.OccurredOn;
        Version++;
    }

    /// <summary>
    /// Applies an InvestmentValueUpdatedEvent to update the current value.
    /// This method is called during event stream replay.
    /// </summary>
    /// <param name="event">The InvestmentValueUpdatedEvent to apply</param>
    public void Apply(InvestmentValueUpdatedEvent @event)
    {
        CurrentValue = @event.NewValue;
        LastUpdated = @event.UpdatedAt;
        Version++;
    }

    /// <summary>
    /// Applies an InvestmentSoldEvent to mark the investment as sold.
    /// This method is called during event stream replay.
    /// </summary>
    /// <param name="event">The InvestmentSoldEvent to apply</param>
    public void Apply(InvestmentSoldEvent @event)
    {
        // Store value per unit BEFORE reducing quantity
        var valuePerUnit = Quantity > 0 ? CurrentValue.Amount / Quantity : 0;
        
        Quantity -= @event.QuantitySold;
        SoldDate = @event.SaleDate;
        RealizedProfitLoss = @event.RealizedProfitLoss;
        
        // If all units sold, mark as sold; otherwise mark as partially sold
        Status = @event.IsCompleteSale ? InvestmentStatus.Sold : InvestmentStatus.PartiallySold;
        
        // If completely sold, current value is 0
        if (@event.IsCompleteSale)
        {
            CurrentValue = new Money(0, CurrentValue.Currency);
        }
        else
        {
            // Recalculate current value for remaining quantity using stored value per unit
            CurrentValue = new Money(valuePerUnit * Quantity, CurrentValue.Currency);
        }
        
        LastUpdated = @event.OccurredOn;
        Version++;
    }

    /// <summary>
    /// Calculates the current unrealized profit or loss.
    /// </summary>
    /// <returns>The unrealized profit/loss (positive = profit, negative = loss)</returns>
    public Money GetUnrealizedProfitLoss()
    {
        if (Status == InvestmentStatus.Sold)
            return new Money(0, CurrentValue.Currency);

        var totalCost = PurchasePrice.Amount * Quantity;
        return new Money(CurrentValue.Amount - totalCost, CurrentValue.Currency);
    }

    /// <summary>
    /// Calculates the return on investment percentage.
    /// </summary>
    /// <returns>The ROI percentage</returns>
    public decimal GetROIPercentage()
    {
        // For sold investments, calculate ROI based on realized profit/loss
        if (Status == InvestmentStatus.Sold && RealizedProfitLoss != null)
        {
            var totalOriginalCost = PurchasePrice.Amount * OriginalQuantity;
            if (totalOriginalCost == 0)
                return 0;
            return (RealizedProfitLoss.Amount / totalOriginalCost) * 100;
        }

        // For active investments, calculate unrealized ROI
        var totalCost = PurchasePrice.Amount * Quantity;
        if (totalCost == 0)
            return 0;

        return ((CurrentValue.Amount - totalCost) / totalCost) * 100;
    }

    /// <summary>
    /// Returns a string representation of the aggregate.
    /// </summary>
    public override string ToString() =>
        $"Investment '{Symbol.Ticker}' - {Quantity} units @ {CurrentValue.Amount / Quantity:F2} {CurrentValue.Currency} " +
        $"(Status: {Status}, ROI: {GetROIPercentage():F2}%, Version: {Version})";
}

