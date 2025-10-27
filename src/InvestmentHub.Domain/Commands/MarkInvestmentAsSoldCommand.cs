using InvestmentHub.Domain.ValueObjects;
using MediatR;

namespace InvestmentHub.Domain.Commands;

/// <summary>
/// Command to mark an investment as sold.
/// This represents a write operation that changes the status of an investment.
/// </summary>
public record MarkInvestmentAsSoldCommand : IRequest<MarkInvestmentAsSoldResult>
{
    /// <summary>
    /// Gets the investment ID to mark as sold.
    /// </summary>
    public InvestmentId InvestmentId { get; init; }

    /// <summary>
    /// Gets the sale price per unit.
    /// </summary>
    public Money SalePrice { get; init; }

    /// <summary>
    /// Gets the sale date.
    /// </summary>
    public DateTime SaleDate { get; init; }

    /// <summary>
    /// Initializes a new instance of the MarkInvestmentAsSoldCommand class.
    /// </summary>
    /// <param name="investmentId">The investment ID</param>
    /// <param name="salePrice">The sale price per unit</param>
    /// <param name="saleDate">The sale date</param>
    public MarkInvestmentAsSoldCommand(InvestmentId investmentId, Money salePrice, DateTime saleDate)
    {
        InvestmentId = investmentId;
        SalePrice = salePrice;
        SaleDate = saleDate;
    }
}

/// <summary>
/// Result of the MarkInvestmentAsSoldCommand operation.
/// </summary>
public record MarkInvestmentAsSoldResult
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
    /// Gets the realized gain/loss from the sale.
    /// </summary>
    public Money? RealizedGainLoss { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="realizedGainLoss">The realized gain/loss</param>
    /// <returns>A successful result</returns>
    public static MarkInvestmentAsSoldResult Success(Money realizedGainLoss) =>
        new() { IsSuccess = true, RealizedGainLoss = realizedGainLoss };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <returns>A failed result</returns>
    public static MarkInvestmentAsSoldResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
