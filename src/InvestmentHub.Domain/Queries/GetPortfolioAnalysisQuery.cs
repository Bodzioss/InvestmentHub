using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Enums;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace InvestmentHub.Domain.Queries;

/// <summary>
/// Query to get comprehensive portfolio analysis including risk and diversification metrics.
/// This represents a read operation that performs complex calculations.
/// </summary>
public record GetPortfolioAnalysisQuery : IRequest<GetPortfolioAnalysisResult>
{
    /// <summary>
    /// Gets the portfolio ID to analyze.
    /// </summary>
    [Required(ErrorMessage = "Portfolio ID is required")]
    public PortfolioId PortfolioId { get; init; }

    /// <summary>
    /// Gets a value indicating whether to include detailed investment breakdown.
    /// </summary>
    public bool IncludeDetailedBreakdown { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether to include historical performance data.
    /// </summary>
    public bool IncludeHistoricalData { get; init; } = false;

    /// <summary>
    /// Initializes a new instance of the GetPortfolioAnalysisQuery class.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID</param>
    /// <param name="includeDetailedBreakdown">Whether to include detailed breakdown</param>
    /// <param name="includeHistoricalData">Whether to include historical data</param>
    public GetPortfolioAnalysisQuery(
        PortfolioId portfolioId,
        bool includeDetailedBreakdown = false,
        bool includeHistoricalData = false)
    {
        PortfolioId = portfolioId;
        IncludeDetailedBreakdown = includeDetailedBreakdown;
        IncludeHistoricalData = includeHistoricalData;
    }
}

/// <summary>
/// Result of the GetPortfolioAnalysisQuery operation.
/// </summary>
public record GetPortfolioAnalysisResult
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
    /// Gets the portfolio analysis data if successful.
    /// </summary>
    public PortfolioAnalysisData? Analysis { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="analysis">The portfolio analysis data</param>
    /// <returns>A successful result</returns>
    public static GetPortfolioAnalysisResult Success(PortfolioAnalysisData analysis) =>
        new() { IsSuccess = true, Analysis = analysis };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <returns>A failed result</returns>
    public static GetPortfolioAnalysisResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Comprehensive portfolio analysis data.
/// </summary>
public record PortfolioAnalysisData
{
    /// <summary>
    /// Gets the portfolio ID.
    /// </summary>
    public PortfolioId PortfolioId { get; init; }

    /// <summary>
    /// Gets the analysis timestamp.
    /// </summary>
    public DateTime AnalysisDate { get; init; }

    /// <summary>
    /// Gets the total portfolio value.
    /// </summary>
    public Money TotalValue { get; init; }

    /// <summary>
    /// Gets the total portfolio cost.
    /// </summary>
    public Money TotalCost { get; init; }

    /// <summary>
    /// Gets the unrealized gain or loss.
    /// </summary>
    public Money UnrealizedGainLoss { get; init; }

    /// <summary>
    /// Gets the percentage return.
    /// </summary>
    public decimal PercentageReturn { get; init; }

    /// <summary>
    /// Gets the number of investments.
    /// </summary>
    public int InvestmentCount { get; init; }

    /// <summary>
    /// Gets the number of unique asset types.
    /// </summary>
    public int AssetTypeCount { get; init; }

    /// <summary>
    /// Gets the concentration risk (percentage in largest asset type).
    /// </summary>
    public decimal ConcentrationRisk { get; init; }

    /// <summary>
    /// Gets the diversification score (0-100).
    /// </summary>
    public decimal DiversificationScore { get; init; }

    /// <summary>
    /// Gets the calculated risk score (0-100).
    /// </summary>
    public decimal RiskScore { get; init; }

    /// <summary>
    /// Gets the determined risk level.
    /// </summary>
    public RiskLevel RiskLevel { get; init; }

    /// <summary>
    /// Gets the asset type breakdown if detailed breakdown is requested.
    /// </summary>
    public IReadOnlyList<AssetTypeBreakdown>? AssetTypeBreakdown { get; init; }

    /// <summary>
    /// Gets the top performing investments if detailed breakdown is requested.
    /// </summary>
    public IReadOnlyList<InvestmentPerformance>? TopPerformers { get; init; }

    /// <summary>
    /// Gets the worst performing investments if detailed breakdown is requested.
    /// </summary>
    public IReadOnlyList<InvestmentPerformance>? WorstPerformers { get; init; }

    /// <summary>
    /// Initializes a new instance of the PortfolioAnalysisData class.
    /// </summary>
    public PortfolioAnalysisData(
        PortfolioId portfolioId,
        DateTime analysisDate,
        Money totalValue,
        Money totalCost,
        Money unrealizedGainLoss,
        decimal percentageReturn,
        int investmentCount,
        int assetTypeCount,
        decimal concentrationRisk,
        decimal diversificationScore,
        decimal riskScore,
        RiskLevel riskLevel,
        IReadOnlyList<AssetTypeBreakdown>? assetTypeBreakdown = null,
        IReadOnlyList<InvestmentPerformance>? topPerformers = null,
        IReadOnlyList<InvestmentPerformance>? worstPerformers = null)
    {
        PortfolioId = portfolioId;
        AnalysisDate = analysisDate;
        TotalValue = totalValue;
        TotalCost = totalCost;
        UnrealizedGainLoss = unrealizedGainLoss;
        PercentageReturn = percentageReturn;
        InvestmentCount = investmentCount;
        AssetTypeCount = assetTypeCount;
        ConcentrationRisk = concentrationRisk;
        DiversificationScore = diversificationScore;
        RiskScore = riskScore;
        RiskLevel = riskLevel;
        AssetTypeBreakdown = assetTypeBreakdown;
        TopPerformers = topPerformers;
        WorstPerformers = worstPerformers;
    }
}

/// <summary>
/// Breakdown of investments by asset type.
/// </summary>
public record AssetTypeBreakdown
{
    /// <summary>
    /// Gets the asset type.
    /// </summary>
    public AssetType AssetType { get; init; }

    /// <summary>
    /// Gets the number of investments of this type.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Gets the total value of investments of this type.
    /// </summary>
    public Money TotalValue { get; init; }

    /// <summary>
    /// Gets the percentage of total portfolio value.
    /// </summary>
    public decimal Percentage { get; init; }

    /// <summary>
    /// Initializes a new instance of the AssetTypeBreakdown class.
    /// </summary>
    public AssetTypeBreakdown(AssetType assetType, int count, Money totalValue, decimal percentage)
    {
        AssetType = assetType;
        Count = count;
        TotalValue = totalValue;
        Percentage = percentage;
    }
}

/// <summary>
/// Performance data for an investment.
/// </summary>
public record InvestmentPerformance
{
    /// <summary>
    /// Gets the investment ID.
    /// </summary>
    public InvestmentId InvestmentId { get; init; }

    /// <summary>
    /// Gets the investment symbol.
    /// </summary>
    public Symbol Symbol { get; init; }

    /// <summary>
    /// Gets the percentage return.
    /// </summary>
    public decimal PercentageReturn { get; init; }

    /// <summary>
    /// Gets the unrealized gain or loss.
    /// </summary>
    public Money UnrealizedGainLoss { get; init; }

    /// <summary>
    /// Initializes a new instance of the InvestmentPerformance class.
    /// </summary>
    public InvestmentPerformance(InvestmentId investmentId, Symbol symbol, decimal percentageReturn, Money unrealizedGainLoss)
    {
        InvestmentId = investmentId;
        Symbol = symbol;
        PercentageReturn = percentageReturn;
        UnrealizedGainLoss = unrealizedGainLoss;
    }
}