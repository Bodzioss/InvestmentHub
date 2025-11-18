using InvestmentHub.Domain.ReadModels;
using InvestmentHub.Domain.ValueObjects;
using MediatR;

namespace InvestmentHub.Domain.Queries;

/// <summary>
/// Query to get a portfolio by its ID from the read model.
/// This represents a read operation that retrieves portfolio data optimized for queries.
/// </summary>
public record GetPortfolioQuery : IRequest<GetPortfolioResult>
{
    /// <summary>
    /// Gets the portfolio ID to retrieve.
    /// </summary>
    public PortfolioId PortfolioId { get; init; }

    /// <summary>
    /// Initializes a new instance of the GetPortfolioQuery class.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID</param>
    public GetPortfolioQuery(PortfolioId portfolioId)
    {
        PortfolioId = portfolioId;
    }
}

/// <summary>
/// Result of the GetPortfolioQuery operation.
/// </summary>
public record GetPortfolioResult
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
    /// Gets the portfolio read model data.
    /// </summary>
    public PortfolioReadModel? Portfolio { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="portfolio">The portfolio read model data</param>
    /// <returns>A successful result</returns>
    public static GetPortfolioResult Success(PortfolioReadModel portfolio) =>
        new() { IsSuccess = true, Portfolio = portfolio };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <returns>A failed result</returns>
    public static GetPortfolioResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
