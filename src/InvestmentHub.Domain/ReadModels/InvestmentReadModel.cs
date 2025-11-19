using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.ReadModels;

/// <summary>
/// Read model for investment queries - denormalized view optimized for reading.
/// This model is automatically built and updated from investment events via InvestmentProjection.
/// </summary>
public class InvestmentReadModel
{
    /// <summary>
    /// Gets or sets the unique identifier of the investment (same as aggregate stream ID).
    /// This is used as the primary key for Marten document storage.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the portfolio identifier that owns this investment.
    /// </summary>
    public Guid PortfolioId { get; set; }

    /// <summary>
    /// Gets or sets the investment ticker symbol.
    /// </summary>
    public string Ticker { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exchange where the investment is traded.
    /// </summary>
    public string Exchange { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the asset type (Stock, Bond, ETF, etc.).
    /// </summary>
    public string AssetType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current market value (total, not per unit).
    /// </summary>
    public decimal CurrentValue { get; set; }

    /// <summary>
    /// Gets or sets the purchase price per unit.
    /// </summary>
    public decimal PurchasePrice { get; set; }

    /// <summary>
    /// Gets or sets the currency code (e.g., USD, EUR).
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current quantity of units held.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the original quantity purchased (for tracking partial sales).
    /// </summary>
    public decimal OriginalQuantity { get; set; }

    /// <summary>
    /// Gets or sets the date when the investment was purchased.
    /// </summary>
    public DateTime PurchaseDate { get; set; }

    /// <summary>
    /// Gets or sets the current status of the investment.
    /// </summary>
    public InvestmentStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the date when the investment was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Gets or sets the date when the investment was sold (null if still active).
    /// </summary>
    public DateTime? SoldDate { get; set; }

    /// <summary>
    /// Gets or sets the realized profit/loss if sold (null if still active).
    /// </summary>
    public decimal? RealizedProfitLoss { get; set; }

    /// <summary>
    /// Gets the total cost basis (purchase price × current quantity).
    /// </summary>
    public decimal TotalCost => PurchasePrice * Quantity;

    /// <summary>
    /// Gets the unrealized profit/loss (current value - total cost).
    /// Returns 0 if the investment is sold.
    /// </summary>
    public decimal UnrealizedProfitLoss
    {
        get
        {
            if (Status == InvestmentStatus.Sold)
                return 0;
            return CurrentValue - TotalCost;
        }
    }

    /// <summary>
    /// Gets the return on investment percentage.
    /// For active investments: ((CurrentValue - TotalCost) / TotalCost) * 100
    /// For sold investments: ROI at time of sale (calculated from RealizedProfitLoss)
    /// </summary>
    public decimal ROIPercentage
    {
        get
        {
            if (Status == InvestmentStatus.Sold && RealizedProfitLoss.HasValue)
            {
                // Calculate ROI from realized P/L
                var originalCost = PurchasePrice * OriginalQuantity;
                return originalCost != 0 ? (RealizedProfitLoss.Value / originalCost) * 100 : 0;
            }

            if (TotalCost == 0)
                return 0;

            return (UnrealizedProfitLoss / TotalCost) * 100;
        }
    }

    /// <summary>
    /// Gets the current value per unit.
    /// </summary>
    public decimal ValuePerUnit => Quantity > 0 ? CurrentValue / Quantity : 0;

    /// <summary>
    /// Initializes a new instance of the InvestmentReadModel class.
    /// </summary>
    public InvestmentReadModel()
    {
    }

    /// <summary>
    /// Returns a string representation of the read model.
    /// </summary>
    public override string ToString() =>
        $"Investment {Ticker} - {Quantity} units @ {ValuePerUnit:F2} {Currency} " +
        $"(Status: {Status}, ROI: {ROIPercentage:F2}%)";
}

