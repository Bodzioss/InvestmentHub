using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Events;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Services;

/// <summary>
/// Service responsible for portfolio valuation calculations and analytics.
/// Provides business logic for portfolio performance analysis and metrics.
/// </summary>
public interface IPortfolioValuationService
{
    /// <summary>
    /// Calculates the total current value of a portfolio.
    /// </summary>
    /// <param name="portfolio">The portfolio to evaluate</param>
    /// <returns>Total current value as Money</returns>
    Task<Money> CalculateTotalValueAsync(Portfolio portfolio);
    
    /// <summary>
    /// Calculates the total cost basis of a portfolio.
    /// </summary>
    /// <param name="portfolio">The portfolio to evaluate</param>
    /// <returns>Total cost basis as Money</returns>
    Task<Money> CalculateTotalCostAsync(Portfolio portfolio);
    
    /// <summary>
    /// Calculates the unrealized gain or loss for a portfolio.
    /// </summary>
    /// <param name="portfolio">The portfolio to evaluate</param>
    /// <returns>Unrealized gain/loss as Money</returns>
    Task<Money> CalculateUnrealizedGainLossAsync(Portfolio portfolio);
    
    /// <summary>
    /// Calculates the percentage return for a portfolio.
    /// </summary>
    /// <param name="portfolio">The portfolio to evaluate</param>
    /// <returns>Percentage return as decimal (e.g., 0.15 for 15% gain)</returns>
    Task<decimal> CalculatePercentageReturnAsync(Portfolio portfolio);
    
    /// <summary>
    /// Calculates portfolio diversification metrics.
    /// </summary>
    /// <param name="portfolio">The portfolio to analyze</param>
    /// <returns>Diversification analysis results</returns>
    Task<PortfolioDiversificationAnalysis> AnalyzeDiversificationAsync(Portfolio portfolio);
    
    /// <summary>
    /// Gets the top performing investments in a portfolio.
    /// </summary>
    /// <param name="portfolio">The portfolio to analyze</param>
    /// <param name="topCount">Number of top performers to return</param>
    /// <returns>Collection of top performing investments</returns>
    Task<IEnumerable<InvestmentPerformance>> GetTopPerformersAsync(Portfolio portfolio, int topCount = 5);
    
    /// <summary>
    /// Gets the worst performing investments in a portfolio.
    /// </summary>
    /// <param name="portfolio">The portfolio to analyze</param>
    /// <param name="bottomCount">Number of worst performers to return</param>
    /// <returns>Collection of worst performing investments</returns>
    Task<IEnumerable<InvestmentPerformance>> GetWorstPerformersAsync(Portfolio portfolio, int bottomCount = 5);
    
    /// <summary>
    /// Calculates portfolio risk metrics.
    /// </summary>
    /// <param name="portfolio">The portfolio to analyze</param>
    /// <returns>Risk analysis results</returns>
    Task<PortfolioRiskAnalysis> AnalyzeRiskAsync(Portfolio portfolio);
    
    /// <summary>
    /// Processes an investment added event for portfolio valuation updates.
    /// </summary>
    /// <param name="investmentAddedEvent">The investment added event to process</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ProcessEvent(InvestmentAddedEvent investmentAddedEvent);
}

/// <summary>
/// Represents the performance of a single investment.
/// </summary>
public class InvestmentPerformance
{
    /// <summary>
    /// Gets the investment ID.
    /// </summary>
    public InvestmentId InvestmentId { get; }
    
    /// <summary>
    /// Gets the investment symbol.
    /// </summary>
    public Symbol Symbol { get; }
    
    /// <summary>
    /// Gets the current value of the investment.
    /// </summary>
    public Money CurrentValue { get; }
    
    /// <summary>
    /// Gets the total cost of the investment.
    /// </summary>
    public Money TotalCost { get; }
    
    /// <summary>
    /// Gets the unrealized gain or loss.
    /// </summary>
    public Money UnrealizedGainLoss { get; }
    
    /// <summary>
    /// Gets the percentage return.
    /// </summary>
    public decimal PercentageReturn { get; }
    
    /// <summary>
    /// Initializes a new instance of the InvestmentPerformance class.
    /// </summary>
    public InvestmentPerformance(
        InvestmentId investmentId,
        Symbol symbol,
        Money currentValue,
        Money totalCost,
        Money unrealizedGainLoss,
        decimal percentageReturn)
    {
        InvestmentId = investmentId;
        Symbol = symbol;
        CurrentValue = currentValue;
        TotalCost = totalCost;
        UnrealizedGainLoss = unrealizedGainLoss;
        PercentageReturn = percentageReturn;
    }
}

/// <summary>
/// Represents diversification analysis results for a portfolio.
/// </summary>
public class PortfolioDiversificationAnalysis
{
    /// <summary>
    /// Gets the number of different asset types in the portfolio.
    /// </summary>
    public int AssetTypeCount { get; }
    
    /// <summary>
    /// Gets the number of different sectors (if available).
    /// </summary>
    public int SectorCount { get; }
    
    /// <summary>
    /// Gets the concentration risk (percentage of portfolio in largest holding).
    /// </summary>
    public decimal ConcentrationRisk { get; }
    
    /// <summary>
    /// Gets the diversification score (0-100, higher is better).
    /// </summary>
    public decimal DiversificationScore { get; }
    
    /// <summary>
    /// Initializes a new instance of the PortfolioDiversificationAnalysis class.
    /// </summary>
    public PortfolioDiversificationAnalysis(
        int assetTypeCount,
        int sectorCount,
        decimal concentrationRisk,
        decimal diversificationScore)
    {
        AssetTypeCount = assetTypeCount;
        SectorCount = sectorCount;
        ConcentrationRisk = concentrationRisk;
        DiversificationScore = diversificationScore;
    }
}

/// <summary>
/// Represents risk analysis results for a portfolio.
/// </summary>
public class PortfolioRiskAnalysis
{
    /// <summary>
    /// Gets the portfolio volatility (standard deviation of returns).
    /// </summary>
    public decimal Volatility { get; }
    
    /// <summary>
    /// Gets the maximum drawdown percentage.
    /// </summary>
    public decimal MaxDrawdown { get; }
    
    /// <summary>
    /// Gets the risk score (0-100, higher is riskier).
    /// </summary>
    public decimal RiskScore { get; }
    
    /// <summary>
    /// Gets the risk level classification.
    /// </summary>
    public RiskLevel RiskLevel { get; }
    
    /// <summary>
    /// Initializes a new instance of the PortfolioRiskAnalysis class.
    /// </summary>
    public PortfolioRiskAnalysis(
        decimal volatility,
        decimal maxDrawdown,
        decimal riskScore,
        RiskLevel riskLevel)
    {
        Volatility = volatility;
        MaxDrawdown = maxDrawdown;
        RiskScore = riskScore;
        RiskLevel = riskLevel;
    }
}
