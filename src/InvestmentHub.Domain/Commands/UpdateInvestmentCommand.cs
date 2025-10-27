using InvestmentHub.Domain.ValueObjects;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace InvestmentHub.Domain.Commands;

/// <summary>
/// Command to update an existing investment.
/// This represents a write operation that modifies investment details.
/// </summary>
public record UpdateInvestmentCommand : IRequest<UpdateInvestmentResult>
{
    /// <summary>
    /// Gets the investment ID to update.
    /// </summary>
    [Required(ErrorMessage = "Investment ID is required")]
    public InvestmentId InvestmentId { get; init; }

    /// <summary>
    /// Gets the new purchase price per unit.
    /// </summary>
    [Required(ErrorMessage = "Purchase price is required")]
    public Money? PurchasePrice { get; init; }

    /// <summary>
    /// Gets the new quantity of units.
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be positive")]
    public decimal? Quantity { get; init; }

    /// <summary>
    /// Gets the new current market price per unit.
    /// </summary>
    [Required(ErrorMessage = "Current price is required")]
    public Money? CurrentPrice { get; init; }

    /// <summary>
    /// Gets the new purchase date.
    /// </summary>
    [Range(typeof(DateTime), "1900-01-01", "2100-12-31", ErrorMessage = "Purchase date must be between 1900 and 2100")]
    public DateTime? PurchaseDate { get; init; }

    /// <summary>
    /// Initializes a new instance of the UpdateInvestmentCommand class.
    /// </summary>
    /// <param name="investmentId">The investment ID</param>
    /// <param name="purchasePrice">The new purchase price (optional)</param>
    /// <param name="quantity">The new quantity (optional)</param>
    /// <param name="currentPrice">The new current price (optional)</param>
    /// <param name="purchaseDate">The new purchase date (optional)</param>
    public UpdateInvestmentCommand(
        InvestmentId investmentId,
        Money? purchasePrice = null,
        decimal? quantity = null,
        Money? currentPrice = null,
        DateTime? purchaseDate = null)
    {
        InvestmentId = investmentId;
        PurchasePrice = purchasePrice;
        Quantity = quantity;
        CurrentPrice = currentPrice;
        PurchaseDate = purchaseDate;
    }
}

/// <summary>
/// Result of the UpdateInvestmentCommand operation.
/// </summary>
public record UpdateInvestmentResult
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
    /// Gets the updated investment ID if successful.
    /// </summary>
    public InvestmentId? InvestmentId { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="investmentId">The updated investment ID</param>
    /// <returns>A successful result</returns>
    public static UpdateInvestmentResult Success(InvestmentId investmentId) =>
        new() { IsSuccess = true, InvestmentId = investmentId };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <returns>A failed result</returns>
    public static UpdateInvestmentResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
