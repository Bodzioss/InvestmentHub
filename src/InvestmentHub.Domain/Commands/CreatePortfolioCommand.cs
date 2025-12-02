using InvestmentHub.Domain.ValueObjects;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace InvestmentHub.Domain.Commands;

/// <summary>
/// Command to create a new portfolio.
/// This represents a write operation that creates a new portfolio.
/// </summary>
public record CreatePortfolioCommand : IRequest<CreatePortfolioResult>
{
    /// <summary>
    /// Gets the portfolio ID.
    /// </summary>
    public PortfolioId PortfolioId { get; init; }

    /// <summary>
    /// Gets the owner user ID.
    /// </summary>
    public UserId OwnerId { get; init; }

    /// <summary>
    /// Gets the portfolio name.
    /// </summary>
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Portfolio name must be between 1 and 100 characters")]
    [RegularExpression(@"^\S.*\S$|^\S$", ErrorMessage = "Portfolio name cannot be empty or only whitespace")]
    public string Name { get; init; }

    /// <summary>
    /// Gets the portfolio description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the portfolio currency.
    /// </summary>
    public string Currency { get; init; }

    /// <summary>
    /// Initializes a new instance of the CreatePortfolioCommand class.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID</param>
    /// <param name="ownerId">The owner user ID</param>
    /// <param name="name">The portfolio name</param>
    /// <param name="description">The portfolio description</param>
    /// <param name="currency">The portfolio currency</param>
    public CreatePortfolioCommand(
        PortfolioId portfolioId,
        UserId ownerId,
        string name,
        string? description = null,
        string currency = "USD")
    {
        PortfolioId = portfolioId;
        OwnerId = ownerId;
        Name = name;
        Description = description;
        Currency = currency;
    }
}

/// <summary>
/// Result of the CreatePortfolioCommand operation.
/// </summary>
public record CreatePortfolioResult
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
    /// Gets the created portfolio ID.
    /// </summary>
    public PortfolioId? PortfolioId { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="portfolioId">The created portfolio ID</param>
    /// <returns>A successful result</returns>
    public static CreatePortfolioResult Success(PortfolioId portfolioId) =>
        new() { IsSuccess = true, PortfolioId = portfolioId };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <returns>A failed result</returns>
    public static CreatePortfolioResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
