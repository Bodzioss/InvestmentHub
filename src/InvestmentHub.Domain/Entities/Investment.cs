using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Entities;

/// <summary>
/// Represents an individual investment holding in a portfolio.
/// Entity that encapsulates investment-related business rules and invariants.
/// </summary>
public class Investment
{
    /// <summary>
    /// Gets the unique identifier of the investment.
    /// </summary>
    public InvestmentId Id { get; private set; }
    
    /// <summary>
    /// Gets the unique identifier of the portfolio this investment belongs to.
    /// </summary>
    public PortfolioId PortfolioId { get; private set; }
    
    /// <summary>
    /// Gets the financial symbol representing the investment.
    /// </summary>
    public Symbol Symbol { get; private set; }
    
    /// <summary>
    /// Gets the current market value of the investment.
    /// </summary>
    public Money CurrentValue { get; private set; }
    
    /// <summary>
    /// Gets the original purchase price per unit.
    /// </summary>
    public Money PurchasePrice { get; private set; }
    
    /// <summary>
    /// Gets the quantity of units held.
    /// </summary>
    public decimal Quantity { get; private set; }
    
    /// <summary>
    /// Gets the date when the investment was purchased.
    /// </summary>
    public DateTime PurchaseDate { get; private set; }
    
    /// <summary>
    /// Gets the date when the investment was last updated.
    /// </summary>
    public DateTime LastUpdated { get; private set; }
    
    /// <summary>
    /// Gets the current status of the investment.
    /// </summary>
    public InvestmentStatus Status { get; private set; }
    
    /// <summary>
    /// Private parameterless constructor for EF Core.
    /// </summary>
    private Investment()
    {
        // EF Core requires a parameterless constructor
        // Values will be set through properties
        Id = null!;
        PortfolioId = null!;
        Symbol = null!;
        CurrentValue = null!;
        PurchasePrice = null!;
    }

    /// <summary>
    /// Initializes a new instance of the Investment class.
    /// </summary>
    /// <param name="id">Unique identifier for the investment</param>
    /// <param name="portfolioId">Unique identifier of the portfolio this investment belongs to</param>
    /// <param name="symbol">Financial symbol representing the investment</param>
    /// <param name="purchasePrice">Price per unit at purchase</param>
    /// <param name="quantity">Number of units purchased</param>
    /// <param name="purchaseDate">Date of purchase</param>
    /// <exception cref="ArgumentException">Thrown when quantity is not positive or purchase date is in the future</exception>
    public Investment(InvestmentId id, PortfolioId portfolioId, Symbol symbol, Money purchasePrice, decimal quantity, DateTime purchaseDate)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));
        
        if (purchaseDate > DateTime.UtcNow)
            throw new ArgumentException("Purchase date cannot be in the future", nameof(purchaseDate));
        
        Id = id ?? throw new ArgumentNullException(nameof(id));
        PortfolioId = portfolioId ?? throw new ArgumentNullException(nameof(portfolioId));
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        PurchasePrice = purchasePrice ?? throw new ArgumentNullException(nameof(purchasePrice));
        Quantity = quantity;
        PurchaseDate = purchaseDate;
        LastUpdated = DateTime.UtcNow;
        Status = InvestmentStatus.Active;
        
        // Initialize current value with purchase price
        CurrentValue = purchasePrice.Multiply(quantity);
    }
    
    /// <summary>
    /// Updates the current market value of the investment.
    /// </summary>
    /// <param name="newCurrentPrice">New current price per unit</param>
    /// <exception cref="InvalidOperationException">Thrown when investment is not active</exception>
    /// <exception cref="ArgumentException">Thrown when new price is negative</exception>
    public void UpdateCurrentValue(Money newCurrentPrice)
    {
        if (Status != InvestmentStatus.Active)
            throw new InvalidOperationException("Cannot update value of inactive investment");
        
        CurrentValue = newCurrentPrice.Multiply(Quantity);
        LastUpdated = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Updates the quantity of the investment (for partial sales or additional purchases).
    /// </summary>
    /// <param name="newQuantity">New quantity of units</param>
    /// <exception cref="InvalidOperationException">Thrown when investment is not active</exception>
    /// <exception cref="ArgumentException">Thrown when new quantity is not positive</exception>
    public void UpdateQuantity(decimal newQuantity)
    {
        if (Status != InvestmentStatus.Active)
            throw new InvalidOperationException("Cannot update quantity of inactive investment");
        
        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(newQuantity));
        
        // Calculate current price per unit before updating quantity
        var currentPricePerUnit = CurrentValue.Amount / Quantity;
        
        Quantity = newQuantity;
        LastUpdated = DateTime.UtcNow;
        
        // Recalculate current value based on new quantity and current price per unit
        CurrentValue = new Money(currentPricePerUnit * newQuantity, CurrentValue.Currency);
    }

    /// <summary>
    /// Updates the purchase price of the investment.
    /// </summary>
    /// <param name="newPurchasePrice">New purchase price per unit</param>
    /// <exception cref="InvalidOperationException">Thrown when investment is not active</exception>
    public void UpdatePurchasePrice(Money newPurchasePrice)
    {
        if (Status != InvestmentStatus.Active)
            throw new InvalidOperationException("Cannot update purchase price of inactive investment");
        
        PurchasePrice = newPurchasePrice;
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the purchase date of the investment.
    /// </summary>
    /// <param name="newPurchaseDate">New purchase date</param>
    /// <exception cref="InvalidOperationException">Thrown when investment is not active</exception>
    /// <exception cref="ArgumentException">Thrown when purchase date is in the future</exception>
    public void UpdatePurchaseDate(DateTime newPurchaseDate)
    {
        if (Status != InvestmentStatus.Active)
            throw new InvalidOperationException("Cannot update purchase date of inactive investment");
        
        if (newPurchaseDate > DateTime.UtcNow)
            throw new ArgumentException("Purchase date cannot be in the future", nameof(newPurchaseDate));
        
        PurchaseDate = newPurchaseDate;
        LastUpdated = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Marks the investment as sold.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when investment is already sold or inactive</exception>
    public void MarkAsSold()
    {
        if (Status != InvestmentStatus.Active)
            throw new InvalidOperationException("Cannot sell inactive investment");
        
        Status = InvestmentStatus.Sold;
        LastUpdated = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Suspends the investment from trading.
    /// </summary>
    public void Suspend()
    {
        if (Status == InvestmentStatus.Sold)
            throw new InvalidOperationException("Cannot suspend sold investment");
        
        Status = InvestmentStatus.Suspended;
        LastUpdated = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Calculates the total cost of the investment (purchase price × quantity).
    /// </summary>
    /// <returns>Total cost as Money</returns>
    public Money GetTotalCost() => PurchasePrice.Multiply(Quantity);
    
    /// <summary>
    /// Calculates the unrealized gain or loss.
    /// </summary>
    /// <returns>Difference between current value and total cost</returns>
    public Money GetUnrealizedGainLoss()
    {
        var currentAmount = CurrentValue.Amount;
        var totalCostAmount = GetTotalCost().Amount;
        var difference = currentAmount - totalCostAmount;
        
        // Return the absolute difference with appropriate sign
        return new Money(Math.Abs(difference), CurrentValue.Currency);
    }
    
    /// <summary>
    /// Calculates the percentage gain or loss.
    /// </summary>
    /// <returns>Percentage gain/loss as decimal (e.g., 0.15 for 15% gain)</returns>
    public decimal GetPercentageGainLoss()
    {
        var totalCost = GetTotalCost();
        if (totalCost.Amount == 0) return 0;
        
        return (CurrentValue.Amount - totalCost.Amount) / totalCost.Amount;
    }
    
    /// <summary>
    /// Determines if the investment is profitable.
    /// </summary>
    /// <returns>True if current value exceeds total cost</returns>
    public bool IsProfitable() => CurrentValue.Amount > GetTotalCost().Amount;
    
    /// <summary>
    /// Returns a string representation of the investment.
    /// </summary>
    /// <returns>Formatted string with investment details</returns>
    public override string ToString() => 
        $"{Symbol.Ticker}: {Quantity} units @ {PurchasePrice} (Current: {CurrentValue}, Status: {Status})";
}
