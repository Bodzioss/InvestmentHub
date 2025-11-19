using InvestmentHub.Domain.Common;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Events;

/// <summary>
/// Domain event raised when a new investment is added to a portfolio.
/// This event captures all initial investment details at the moment of purchase.
/// </summary>
public class InvestmentAddedEvent : DomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the portfolio containing this investment.
    /// </summary>
    public PortfolioId PortfolioId { get; }

    /// <summary>
    /// Gets the unique identifier of the investment.
    /// </summary>
    public InvestmentId InvestmentId { get; }

    /// <summary>
    /// Gets the symbol (ticker) of the investment.
    /// </summary>
    public Symbol Symbol { get; }

    /// <summary>
    /// Gets the purchase price per unit.
    /// </summary>
    public Money PurchasePrice { get; }

    /// <summary>
    /// Gets the quantity of units purchased.
    /// </summary>
    public decimal Quantity { get; }

    /// <summary>
    /// Gets the date when the investment was purchased.
    /// </summary>
    public DateTime PurchaseDate { get; }

    /// <summary>
    /// Gets the initial current value (typically same as purchase price at creation).
    /// </summary>
    public Money InitialCurrentValue { get; }

    /// <summary>
    /// Initializes a new instance of the InvestmentAddedEvent class.
    /// </summary>
    /// <param name="portfolioId">The portfolio identifier</param>
    /// <param name="investmentId">The investment identifier</param>
    /// <param name="symbol">The investment symbol</param>
    /// <param name="purchasePrice">The purchase price per unit</param>
    /// <param name="quantity">The quantity purchased</param>
    /// <param name="purchaseDate">The purchase date</param>
    /// <param name="initialCurrentValue">The initial current value</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    /// <exception cref="ArgumentException">Thrown when quantity or prices are invalid</exception>
    public InvestmentAddedEvent(
        PortfolioId portfolioId,
        InvestmentId investmentId,
        Symbol symbol,
        Money purchasePrice,
        decimal quantity,
        DateTime purchaseDate,
        Money initialCurrentValue) : base(version: 1)
    {
        PortfolioId = portfolioId ?? throw new ArgumentNullException(nameof(portfolioId));
        InvestmentId = investmentId ?? throw new ArgumentNullException(nameof(investmentId));
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        PurchasePrice = purchasePrice ?? throw new ArgumentNullException(nameof(purchasePrice));
        InitialCurrentValue = initialCurrentValue ?? throw new ArgumentNullException(nameof(initialCurrentValue));

        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        }

        if (purchasePrice.Amount <= 0)
        {
            throw new ArgumentException("Purchase price must be greater than zero", nameof(purchasePrice));
        }

        if (purchaseDate > DateTime.UtcNow)
        {
            throw new ArgumentException("Purchase date cannot be in the future", nameof(purchaseDate));
        }

        Quantity = quantity;
        PurchaseDate = purchaseDate;
    }

    /// <summary>
    /// Returns a string representation of the event.
    /// </summary>
    public override string ToString() =>
        $"InvestmentAdded: {Symbol.Ticker} x{Quantity} @ {PurchasePrice.Amount} {PurchasePrice.Currency} to Portfolio {PortfolioId.Value}";
}
