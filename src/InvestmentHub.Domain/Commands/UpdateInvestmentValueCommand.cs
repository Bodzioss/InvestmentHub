using InvestmentHub.Domain.ValueObjects;
using MediatR;

namespace InvestmentHub.Domain.Commands;

/// <summary>
/// Command to update an existing investment's current value.
/// This represents a write operation that changes the market value of an investment.
/// </summary>
public record UpdateInvestmentValueCommand : IRequest<UpdateInvestmentValueResult>
{
    /// <summary>
    /// Gets the investment ID to update.
    /// </summary>
    public InvestmentId InvestmentId { get; init; }

    /// <summary>
    /// Gets the new current price per unit.
    /// </summary>
    public Money CurrentPrice { get; init; }

    /// <summary>
    /// Initializes a new instance of the UpdateInvestmentValueCommand class.
    /// </summary>
    /// <param name="investmentId">The investment ID</param>
    /// <param name="currentPrice">The new current price per unit</param>
    public UpdateInvestmentValueCommand(InvestmentId investmentId, Money currentPrice)
    {
        InvestmentId = investmentId;
        CurrentPrice = currentPrice;
    }
}

/// <summary>
/// Result of the UpdateInvestmentValueCommand operation.
/// </summary>
public record UpdateInvestmentValueResult
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
    /// Gets the updated current value of the investment.
    /// </summary>
    public Money? UpdatedValue { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="updatedValue">The updated current value</param>
    /// <returns>A successful result</returns>
    public static UpdateInvestmentValueResult Success(Money updatedValue) =>
        new() { IsSuccess = true, UpdatedValue = updatedValue };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <returns>A failed result</returns>
    public static UpdateInvestmentValueResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
