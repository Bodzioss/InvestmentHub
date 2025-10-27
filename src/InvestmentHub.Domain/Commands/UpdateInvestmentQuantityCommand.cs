using InvestmentHub.Domain.ValueObjects;
using MediatR;

namespace InvestmentHub.Domain.Commands;

/// <summary>
/// Command to update an investment's quantity (for partial sales or additional purchases).
/// This represents a write operation that changes the quantity of an investment.
/// </summary>
public record UpdateInvestmentQuantityCommand : IRequest<UpdateInvestmentQuantityResult>
{
    /// <summary>
    /// Gets the investment ID to update.
    /// </summary>
    public InvestmentId InvestmentId { get; init; }

    /// <summary>
    /// Gets the new quantity of units.
    /// </summary>
    public decimal NewQuantity { get; init; }

    /// <summary>
    /// Initializes a new instance of the UpdateInvestmentQuantityCommand class.
    /// </summary>
    /// <param name="investmentId">The investment ID</param>
    /// <param name="newQuantity">The new quantity of units</param>
    public UpdateInvestmentQuantityCommand(InvestmentId investmentId, decimal newQuantity)
    {
        InvestmentId = investmentId;
        NewQuantity = newQuantity;
    }
}

/// <summary>
/// Result of the UpdateInvestmentQuantityCommand operation.
/// </summary>
public record UpdateInvestmentQuantityResult
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
    public static UpdateInvestmentQuantityResult Success(Money updatedValue) =>
        new() { IsSuccess = true, UpdatedValue = updatedValue };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <returns>A failed result</returns>
    public static UpdateInvestmentQuantityResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
