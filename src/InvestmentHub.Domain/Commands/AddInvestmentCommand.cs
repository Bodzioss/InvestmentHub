using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.ValueObjects;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace InvestmentHub.Domain.Commands;

/// <summary>
/// Command to add a new investment to a portfolio.
/// This represents a write operation that changes the state of the system.
/// </summary>
public record AddInvestmentCommand : IRequest<AddInvestmentResult>
{
    /// <summary>
    /// Gets the portfolio ID where the investment will be added.
    /// </summary>
    public PortfolioId PortfolioId { get; init; }

    /// <summary>
    /// Gets the investment symbol (ticker, exchange, asset type).
    /// </summary>
    public Symbol Symbol { get; init; }

    /// <summary>
    /// Gets the purchase price per unit.
    /// </summary>
    public Money PurchasePrice { get; init; }

    /// <summary>
    /// Gets the quantity of units purchased.
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be positive")]
    public decimal Quantity { get; init; }

    /// <summary>
    /// Gets the purchase date.
    /// </summary>
    [Required(ErrorMessage = "Purchase date is required")]
    [Range(typeof(DateTime), "1900-01-01", "2100-12-31", ErrorMessage = "Purchase date must be between 1900 and 2100")]
    public DateTime PurchaseDate { get; init; }

    /// <summary>
    /// Initializes a new instance of the AddInvestmentCommand class.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID</param>
    /// <param name="symbol">The investment symbol</param>
    /// <param name="purchasePrice">The purchase price per unit</param>
    /// <param name="quantity">The quantity of units</param>
    /// <param name="purchaseDate">The purchase date</param>
    public AddInvestmentCommand(
        PortfolioId portfolioId,
        Symbol symbol,
        Money purchasePrice,
        decimal quantity,
        DateTime purchaseDate)
    {
        PortfolioId = portfolioId;
        Symbol = symbol;
        PurchasePrice = purchasePrice;
        Quantity = quantity;
        PurchaseDate = purchaseDate;
    }
}

/// <summary>
/// Result of the AddInvestmentCommand operation.
/// </summary>
public record AddInvestmentResult
{
    /// <summary>
    /// Gets the created investment ID.
    /// </summary>
    public InvestmentId InvestmentId { get; init; }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="investmentId">The created investment ID</param>
    /// <returns>A successful result</returns>
    public static AddInvestmentResult Success(InvestmentId investmentId) =>
        new() { InvestmentId = investmentId, IsSuccess = true };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <returns>A failed result</returns>
    public static AddInvestmentResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
