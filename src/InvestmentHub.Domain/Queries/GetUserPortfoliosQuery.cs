using InvestmentHub.Domain.ValueObjects;
using MediatR;

namespace InvestmentHub.Domain.Queries;

/// <summary>
/// Query to get all portfolios for a specific user.
/// This represents a read operation that retrieves multiple portfolios.
/// </summary>
public record GetUserPortfoliosQuery : IRequest<GetUserPortfoliosResult>
{
    /// <summary>
    /// Gets the user ID to retrieve portfolios for.
    /// </summary>
    public UserId UserId { get; init; }

    /// <summary>
    /// Initializes a new instance of the GetUserPortfoliosQuery class.
    /// </summary>
    /// <param name="userId">The user ID</param>
    public GetUserPortfoliosQuery(UserId userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// Result of the GetUserPortfoliosQuery operation.
/// </summary>
public record GetUserPortfoliosResult
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
    /// Gets the list of portfolios.
    /// </summary>
    public IReadOnlyList<PortfolioSummary> Portfolios { get; init; } = new List<PortfolioSummary>();

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="portfolios">The list of portfolios</param>
    /// <returns>A successful result</returns>
    public static GetUserPortfoliosResult Success(IReadOnlyList<PortfolioSummary> portfolios) =>
        new() { IsSuccess = true, Portfolios = portfolios };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <returns>A failed result</returns>
    public static GetUserPortfoliosResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Summary information about a portfolio for listing purposes.
/// </summary>
public record PortfolioSummary
{
    /// <summary>
    /// Gets the portfolio ID.
    /// </summary>
    public PortfolioId PortfolioId { get; init; }

    /// <summary>
    /// Gets the portfolio name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Gets the portfolio description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the total value of the portfolio.
    /// </summary>
    public Money TotalValue { get; init; }

    /// <summary>
    /// Gets the total cost of the portfolio.
    /// </summary>
    public Money TotalCost { get; init; }

    /// <summary>
    /// Gets the unrealized gain/loss.
    /// </summary>
    public Money UnrealizedGainLoss { get; init; }

    /// <summary>
    /// Gets the number of active investments.
    /// </summary>
    public int ActiveInvestmentCount { get; init; }

    /// <summary>
    /// Initializes a new instance of the PortfolioSummary class.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID</param>
    /// <param name="name">The portfolio name</param>
    /// <param name="description">The portfolio description</param>
    /// <param name="totalValue">The total value</param>
    /// <param name="totalCost">The total cost</param>
    /// <param name="unrealizedGainLoss">The unrealized gain/loss</param>
    /// <param name="activeInvestmentCount">The active investment count</param>
    public PortfolioSummary(
        PortfolioId portfolioId,
        string name,
        string? description,
        Money totalValue,
        Money totalCost,
        Money unrealizedGainLoss,
        int activeInvestmentCount)
    {
        PortfolioId = portfolioId;
        Name = name;
        Description = description;
        TotalValue = totalValue;
        TotalCost = totalCost;
        UnrealizedGainLoss = unrealizedGainLoss;
        ActiveInvestmentCount = activeInvestmentCount;
    }
}
