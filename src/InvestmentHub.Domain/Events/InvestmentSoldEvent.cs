using InvestmentHub.Domain.Common;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Events;

/// <summary>
/// Domain event raised when an investment is sold (fully or partially).
/// This event captures the sale transaction details and realized profit/loss.
/// </summary>
public class InvestmentSoldEvent : DomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the investment being sold.
    /// </summary>
    public InvestmentId InvestmentId { get; }

    /// <summary>
    /// Gets the portfolio identifier (for cross-stream projections).
    /// </summary>
    public PortfolioId PortfolioId { get; }

    /// <summary>
    /// Gets the sale price per unit.
    /// </summary>
    public Money SalePrice { get; }

    /// <summary>
    /// Gets the quantity of units sold.
    /// </summary>
    public decimal QuantitySold { get; }

    /// <summary>
    /// Gets the date when the investment was sold.
    /// </summary>
    public DateTime SaleDate { get; }

    /// <summary>
    /// Gets the purchase price per unit (for profit/loss calculation).
    /// </summary>
    public Money PurchasePrice { get; }

    /// <summary>
    /// Gets the total sale proceeds (SalePrice * QuantitySold).
    /// </summary>
    public Money TotalProceeds { get; }

    /// <summary>
    /// Gets the total cost basis (PurchasePrice * QuantitySold).
    /// </summary>
    public Money TotalCost { get; }

    /// <summary>
    /// Gets the realized profit or loss from the sale.
    /// Positive value = profit, negative value = loss.
    /// </summary>
    public Money RealizedProfitLoss { get; }

    /// <summary>
    /// Gets the return on investment percentage.
    /// </summary>
    public decimal ROIPercentage { get; }

    /// <summary>
    /// Gets a value indicating whether this was a complete sale (all units sold).
    /// </summary>
    public bool IsCompleteSale { get; }

    /// <summary>
    /// Initializes a new instance of the InvestmentSoldEvent class.
    /// </summary>
    /// <param name="investmentId">The investment identifier</param>
    /// <param name="portfolioId">The portfolio identifier</param>
    /// <param name="salePrice">The sale price per unit</param>
    /// <param name="quantitySold">The quantity sold</param>
    /// <param name="saleDate">The sale date</param>
    /// <param name="purchasePrice">The original purchase price per unit</param>
    /// <param name="isCompleteSale">Whether all units were sold</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    /// <exception cref="ArgumentException">Thrown when values are invalid</exception>
    public InvestmentSoldEvent(
        InvestmentId investmentId,
        PortfolioId portfolioId,
        Money salePrice,
        decimal quantitySold,
        DateTime saleDate,
        Money purchasePrice,
        bool isCompleteSale) : base(version: 1)
    {
        InvestmentId = investmentId ?? throw new ArgumentNullException(nameof(investmentId));
        PortfolioId = portfolioId ?? throw new ArgumentNullException(nameof(portfolioId));
        SalePrice = salePrice ?? throw new ArgumentNullException(nameof(salePrice));
        PurchasePrice = purchasePrice ?? throw new ArgumentNullException(nameof(purchasePrice));

        if (quantitySold <= 0)
        {
            throw new ArgumentException("Quantity sold must be greater than zero", nameof(quantitySold));
        }

        if (salePrice.Amount < 0)
        {
            throw new ArgumentException("Sale price cannot be negative", nameof(salePrice));
        }

        if (salePrice.Currency != purchasePrice.Currency)
        {
            throw new ArgumentException("Currency mismatch between sale and purchase price", nameof(salePrice));
        }

        if (saleDate > DateTime.UtcNow)
        {
            throw new ArgumentException("Sale date cannot be in the future", nameof(saleDate));
        }

        QuantitySold = quantitySold;
        SaleDate = saleDate;
        IsCompleteSale = isCompleteSale;

        // Calculate totals
        TotalProceeds = new Money(salePrice.Amount * quantitySold, salePrice.Currency);
        TotalCost = new Money(purchasePrice.Amount * quantitySold, purchasePrice.Currency);

        // Calculate realized profit/loss
        RealizedProfitLoss = new Money(
            TotalProceeds.Amount - TotalCost.Amount,
            salePrice.Currency);

        // Calculate ROI percentage
        ROIPercentage = TotalCost.Amount != 0
            ? (RealizedProfitLoss.Amount / TotalCost.Amount) * 100
            : 0;
    }

    /// <summary>
    /// Returns a string representation of the event.
    /// </summary>
    public override string ToString() =>
        $"InvestmentSold: {InvestmentId.Value} - {QuantitySold} units @ {SalePrice.Amount} {SalePrice.Currency} " +
        $"(P/L: {RealizedProfitLoss.Amount:F2} {RealizedProfitLoss.Currency}, ROI: {ROIPercentage:F2}%)";
}

