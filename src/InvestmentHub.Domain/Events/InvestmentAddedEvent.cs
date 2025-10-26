using InvestmentHub.Domain.Common;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Events;

/// <summary>
/// Domain event raised when a new investment is added to a portfolio.
/// This event enables decoupled handling of investment additions across the system.
/// </summary>
public class InvestmentAddedEvent : DomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the portfolio where the investment was added.
    /// </summary>
    public PortfolioId PortfolioId { get; }
    
    /// <summary>
    /// Gets the unique identifier of the added investment.
    /// </summary>
    public InvestmentId InvestmentId { get; }
    
    /// <summary>
    /// Gets the symbol of the added investment.
    /// </summary>
    public Symbol Symbol { get; }
    
    /// <summary>
    /// Gets the quantity of the added investment.
    /// </summary>
    public decimal Quantity { get; }
    
    /// <summary>
    /// Gets the purchase price per unit of the investment.
    /// </summary>
    public Money PurchasePrice { get; }
    
    /// <summary>
    /// Gets the total cost of the investment (purchase price × quantity).
    /// </summary>
    public Money TotalCost { get; }
    
    /// <summary>
    /// Gets the date when the investment was purchased.
    /// </summary>
    public DateTime PurchaseDate { get; }
    
    /// <summary>
    /// Gets the unique identifier of the portfolio owner.
    /// </summary>
    public UserId OwnerId { get; }
    
    /// <summary>
    /// Initializes a new instance of the InvestmentAddedEvent class.
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <param name="investmentId">The ID of the investment</param>
    /// <param name="symbol">The investment symbol</param>
    /// <param name="quantity">The quantity purchased</param>
    /// <param name="purchasePrice">The price per unit</param>
    /// <param name="totalCost">The total cost of the investment</param>
    /// <param name="purchaseDate">The purchase date</param>
    /// <param name="ownerId">The ID of the portfolio owner</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null</exception>
    public InvestmentAddedEvent(
        PortfolioId portfolioId,
        InvestmentId investmentId,
        Symbol symbol,
        decimal quantity,
        Money purchasePrice,
        Money totalCost,
        DateTime purchaseDate,
        UserId ownerId) : base(1)
    {
        PortfolioId = portfolioId ?? throw new ArgumentNullException(nameof(portfolioId));
        InvestmentId = investmentId ?? throw new ArgumentNullException(nameof(investmentId));
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        Quantity = quantity;
        PurchasePrice = purchasePrice ?? throw new ArgumentNullException(nameof(purchasePrice));
        TotalCost = totalCost ?? throw new ArgumentNullException(nameof(totalCost));
        PurchaseDate = purchaseDate;
        OwnerId = ownerId ?? throw new ArgumentNullException(nameof(ownerId));
    }
    
    /// <summary>
    /// Creates an InvestmentAddedEvent from an Investment and Portfolio.
    /// </summary>
    /// <param name="investment">The investment that was added</param>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <param name="ownerId">The ID of the portfolio owner</param>
    /// <returns>A new InvestmentAddedEvent instance</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
    public static InvestmentAddedEvent FromInvestment(
        Investment investment,
        PortfolioId portfolioId,
        UserId ownerId)
    {
        if (investment == null)
            throw new ArgumentNullException(nameof(investment));
        
        return new InvestmentAddedEvent(
            portfolioId,
            investment.Id,
            investment.Symbol,
            investment.Quantity,
            investment.PurchasePrice,
            investment.GetTotalCost(),
            investment.PurchaseDate,
            ownerId);
    }
    
    /// <summary>
    /// Returns a string representation of the InvestmentAddedEvent.
    /// </summary>
    /// <returns>Formatted string with event details</returns>
    public override string ToString() => 
        $"InvestmentAdded: {Symbol.Ticker} ({Quantity} units @ {PurchasePrice}) to Portfolio {PortfolioId.Value}";
}
