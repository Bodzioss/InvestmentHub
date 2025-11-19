using InvestmentHub.Domain.ValueObjects;
using MediatR;

namespace InvestmentHub.Domain.Commands;

/// <summary>
/// Command to sell an investment (fully or partially).
/// This represents a write operation that records the sale of an investment.
/// </summary>
public record SellInvestmentCommand : IRequest<SellInvestmentResult>
{
    /// <summary>
    /// Gets the investment ID to sell.
    /// </summary>
    public InvestmentId InvestmentId { get; init; }

    /// <summary>
    /// Gets the sale price per unit.
    /// </summary>
    public Money SalePrice { get; init; }

    /// <summary>
    /// Gets the quantity to sell (null = sell all remaining units).
    /// </summary>
    public decimal? QuantityToSell { get; init; }

    /// <summary>
    /// Gets the sale date.
    /// </summary>
    public DateTime SaleDate { get; init; }

    /// <summary>
    /// Initializes a new instance of the SellInvestmentCommand class.
    /// </summary>
    /// <param name="investmentId">The investment ID</param>
    /// <param name="salePrice">The sale price per unit</param>
    /// <param name="quantityToSell">The quantity to sell (null = sell all)</param>
    /// <param name="saleDate">The sale date</param>
    public SellInvestmentCommand(
        InvestmentId investmentId,
        Money salePrice,
        decimal? quantityToSell,
        DateTime saleDate)
    {
        InvestmentId = investmentId;
        SalePrice = salePrice;
        QuantityToSell = quantityToSell;
        SaleDate = saleDate;
    }
}

/// <summary>
/// Result of the SellInvestmentCommand operation.
/// </summary>
public record SellInvestmentResult
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the realized profit or loss from the sale.
    /// </summary>
    public Money? RealizedProfitLoss { get; init; }

    /// <summary>
    /// Gets the quantity that was sold.
    /// </summary>
    public decimal QuantitySold { get; init; }

    /// <summary>
    /// Gets a value indicating whether this was a complete sale (all units sold).
    /// </summary>
    public bool IsCompleteSale { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="realizedProfitLoss">The realized profit/loss</param>
    /// <param name="quantitySold">The quantity sold</param>
    /// <param name="isCompleteSale">Whether all units were sold</param>
    /// <returns>A successful result</returns>
    public static SellInvestmentResult Success(Money realizedProfitLoss, decimal quantitySold, bool isCompleteSale) =>
        new()
        {
            IsSuccess = true,
            RealizedProfitLoss = realizedProfitLoss,
            QuantitySold = quantitySold,
            IsCompleteSale = isCompleteSale
        };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <returns>A failed result</returns>
    public static SellInvestmentResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}

