using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Enums;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace InvestmentHub.Domain.Queries;

/// <summary>
/// Query to get all investments for a specific portfolio.
/// This represents a read operation that retrieves investment data.
/// </summary>
public record GetInvestmentsQuery : IRequest<GetInvestmentsResult>
{
    /// <summary>
    /// Gets the portfolio ID to retrieve investments for.
    /// </summary>
    [Required(ErrorMessage = "Portfolio ID is required")]
    public PortfolioId PortfolioId { get; init; }

    /// <summary>
    /// Gets the asset type filter (optional).
    /// </summary>
    public AssetType? AssetTypeFilter { get; init; }

    /// <summary>
    /// Gets the status filter (optional).
    /// </summary>
    public InvestmentStatus? StatusFilter { get; init; }

    /// <summary>
    /// Gets a value indicating whether to include inactive investments.
    /// </summary>
    public bool IncludeInactive { get; init; } = false;

    /// <summary>
    /// Gets the maximum number of results to return.
    /// </summary>
    [Range(1, 1000, ErrorMessage = "Limit must be between 1 and 1000")]
    public int Limit { get; init; } = 100;

    /// <summary>
    /// Gets the number of results to skip.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Offset must be non-negative")]
    public int Offset { get; init; } = 0;

    /// <summary>
    /// Initializes a new instance of the GetInvestmentsQuery class.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID</param>
    /// <param name="assetTypeFilter">Optional asset type filter</param>
    /// <param name="statusFilter">Optional status filter</param>
    /// <param name="includeInactive">Whether to include inactive investments</param>
    /// <param name="limit">Maximum number of results</param>
    /// <param name="offset">Number of results to skip</param>
    public GetInvestmentsQuery(
        PortfolioId portfolioId,
        AssetType? assetTypeFilter = null,
        InvestmentStatus? statusFilter = null,
        bool includeInactive = false,
        int limit = 100,
        int offset = 0)
    {
        PortfolioId = portfolioId;
        AssetTypeFilter = assetTypeFilter;
        StatusFilter = statusFilter;
        IncludeInactive = includeInactive;
        Limit = limit;
        Offset = offset;
    }
}

/// <summary>
/// Result of the GetInvestmentsQuery operation.
/// </summary>
public record GetInvestmentsResult
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
    /// Gets the list of investments if successful.
    /// </summary>
    public IReadOnlyList<InvestmentSummary>? Investments { get; init; }

    /// <summary>
    /// Gets the total count of investments matching the criteria.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="investments">The list of investments</param>
    /// <param name="totalCount">The total count</param>
    /// <returns>A successful result</returns>
    public static GetInvestmentsResult Success(IReadOnlyList<InvestmentSummary> investments, int totalCount) =>
        new() { IsSuccess = true, Investments = investments, TotalCount = totalCount };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <returns>A failed result</returns>
    public static GetInvestmentsResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Summary information about an investment for query results.
/// </summary>
public record InvestmentSummary
{
    /// <summary>
    /// Gets the investment ID.
    /// </summary>
    public InvestmentId Id { get; init; }

    /// <summary>
    /// Gets the investment symbol.
    /// </summary>
    public Symbol Symbol { get; init; }

    /// <summary>
    /// Gets the purchase price per unit.
    /// </summary>
    public Money PurchasePrice { get; init; }

    /// <summary>
    /// Gets the current market price per unit.
    /// </summary>
    public Money CurrentPrice { get; init; }

    /// <summary>
    /// Gets the quantity of units.
    /// </summary>
    public decimal Quantity { get; init; }

    /// <summary>
    /// Gets the purchase date.
    /// </summary>
    public DateTime PurchaseDate { get; init; }

    /// <summary>
    /// Gets the investment status.
    /// </summary>
    public InvestmentStatus Status { get; init; }

    /// <summary>
    /// Gets the last updated timestamp.
    /// </summary>
    public DateTime LastUpdated { get; init; }

    /// <summary>
    /// Gets the total cost of the investment.
    /// </summary>
    public Money TotalCost { get; init; }

    /// <summary>
    /// Gets the current value of the investment.
    /// </summary>
    public Money CurrentValue { get; init; }

    /// <summary>
    /// Gets the unrealized gain or loss.
    /// </summary>
    public Money UnrealizedGainLoss { get; init; }

    /// <summary>
    /// Gets the percentage return.
    /// </summary>
    public decimal PercentageReturn { get; init; }

    /// <summary>
    /// Initializes a new instance of the InvestmentSummary class.
    /// </summary>
    public InvestmentSummary(
        InvestmentId id,
        Symbol symbol,
        Money purchasePrice,
        Money currentPrice,
        decimal quantity,
        DateTime purchaseDate,
        InvestmentStatus status,
        DateTime lastUpdated,
        Money totalCost,
        Money currentValue,
        Money unrealizedGainLoss,
        decimal percentageReturn)
    {
        Id = id;
        Symbol = symbol;
        PurchasePrice = purchasePrice;
        CurrentPrice = currentPrice;
        Quantity = quantity;
        PurchaseDate = purchaseDate;
        Status = status;
        LastUpdated = lastUpdated;
        TotalCost = totalCost;
        CurrentValue = currentValue;
        UnrealizedGainLoss = unrealizedGainLoss;
        PercentageReturn = percentageReturn;
    }
}
