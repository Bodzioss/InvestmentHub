using InvestmentHub.Domain.ValueObjects;
using MediatR;

namespace InvestmentHub.Domain.Commands;

/// <summary>
/// Command to update portfolio details (name and description).
/// This represents a write operation that updates portfolio metadata.
/// </summary>
public record UpdatePortfolioDetailsCommand : IRequest<UpdatePortfolioDetailsResult>
{
    /// <summary>
    /// Gets the portfolio ID to update.
    /// </summary>
    public PortfolioId PortfolioId { get; init; }

    /// <summary>
    /// Gets the new portfolio name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Gets the new portfolio description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Initializes a new instance of the UpdatePortfolioDetailsCommand class.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID</param>
    /// <param name="name">The new portfolio name</param>
    /// <param name="description">The new portfolio description</param>
    public UpdatePortfolioDetailsCommand(
        PortfolioId portfolioId,
        string name,
        string? description = null)
    {
        PortfolioId = portfolioId;
        Name = name;
        Description = description;
    }
}

/// <summary>
/// Result of the UpdatePortfolioDetailsCommand operation.
/// </summary>
public record UpdatePortfolioDetailsResult
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
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result</returns>
    public static UpdatePortfolioDetailsResult Success() =>
        new() { IsSuccess = true };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <returns>A failed result</returns>
    public static UpdatePortfolioDetailsResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
