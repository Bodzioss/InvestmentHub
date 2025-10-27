using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.ValueObjects;
using MediatR;

namespace InvestmentHub.Domain.Queries;

/// <summary>
/// Query to get an investment by its ID.
/// This represents a read operation that retrieves investment data.
/// </summary>
public record GetInvestmentQuery : IRequest<GetInvestmentResult>
{
    /// <summary>
    /// Gets the investment ID to retrieve.
    /// </summary>
    public InvestmentId InvestmentId { get; init; }

    /// <summary>
    /// Initializes a new instance of the GetInvestmentQuery class.
    /// </summary>
    /// <param name="investmentId">The investment ID</param>
    public GetInvestmentQuery(InvestmentId investmentId)
    {
        InvestmentId = investmentId;
    }
}

/// <summary>
/// Result of the GetInvestmentQuery operation.
/// </summary>
public record GetInvestmentResult
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
    /// Gets the investment data.
    /// </summary>
    public Investment? Investment { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="investment">The investment data</param>
    /// <returns>A successful result</returns>
    public static GetInvestmentResult Success(Investment investment) =>
        new() { IsSuccess = true, Investment = investment };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <returns>A failed result</returns>
    public static GetInvestmentResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
