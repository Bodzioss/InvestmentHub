using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Events;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Enums;

namespace InvestmentHub.Domain.Services;

/// <summary>
/// Implementation of portfolio valuation service that provides comprehensive portfolio analysis.
/// Handles calculations for portfolio performance, diversification, and risk metrics.
/// </summary>
public class PortfolioValuationService : IPortfolioValuationService
{
    /// <summary>
    /// Calculates the total current value of a portfolio.
    /// </summary>
    /// <param name="portfolio">The portfolio to evaluate</param>
    /// <returns>Total current value as Money</returns>
    public async Task<Money> CalculateTotalValueAsync(Portfolio portfolio)
    {
        if (portfolio == null)
            throw new ArgumentNullException(nameof(portfolio));
        
        // Use the portfolio's built-in calculation
        return await Task.FromResult(portfolio.GetTotalValue());
    }
    
    /// <summary>
    /// Calculates the total cost basis of a portfolio.
    /// </summary>
    /// <param name="portfolio">The portfolio to evaluate</param>
    /// <returns>Total cost basis as Money</returns>
    public async Task<Money> CalculateTotalCostAsync(Portfolio portfolio)
    {
        if (portfolio == null)
            throw new ArgumentNullException(nameof(portfolio));
        
        // Use the portfolio's built-in calculation
        return await Task.FromResult(portfolio.GetTotalCost());
    }
    
    /// <summary>
    /// Calculates the unrealized gain or loss for a portfolio.
    /// </summary>
    /// <param name="portfolio">The portfolio to evaluate</param>
    /// <returns>Unrealized gain/loss as Money</returns>
    public async Task<Money> CalculateUnrealizedGainLossAsync(Portfolio portfolio)
    {
        if (portfolio == null)
            throw new ArgumentNullException(nameof(portfolio));
        
        // Use the portfolio's built-in calculation
        return await Task.FromResult(portfolio.GetTotalGainLoss());
    }
    
    /// <summary>
    /// Calculates the percentage return for a portfolio.
    /// </summary>
    /// <param name="portfolio">The portfolio to evaluate</param>
    /// <returns>Percentage return as decimal (e.g., 0.15 for 15% gain)</returns>
    public async Task<decimal> CalculatePercentageReturnAsync(Portfolio portfolio)
    {
        if (portfolio == null)
            throw new ArgumentNullException(nameof(portfolio));
        
        // Use the portfolio's built-in calculation
        return await Task.FromResult(portfolio.GetPercentageGainLoss());
    }
    
    /// <summary>
    /// Calculates portfolio diversification metrics.
    /// </summary>
    /// <param name="portfolio">The portfolio to analyze</param>
    /// <returns>Diversification analysis results</returns>
    public async Task<PortfolioDiversificationAnalysis> AnalyzeDiversificationAsync(Portfolio portfolio)
    {
        if (portfolio == null)
            throw new ArgumentNullException(nameof(portfolio));
        
        var activeInvestments = portfolio.Investments.Where(i => i.Status == InvestmentStatus.Active).ToList();
        
        if (!activeInvestments.Any())
        {
            return new PortfolioDiversificationAnalysis(0, 0, 0, 0);
        }
        
        // Count different asset types
        var assetTypeCount = activeInvestments.Select(i => i.Symbol.AssetType).Distinct().Count();
        
        // For now, we don't have sector information, so set to 0
        var sectorCount = 0;
        
        // Calculate concentration risk (percentage in largest asset type)
        var totalValue = portfolio.GetTotalValue();
        var assetTypeGroups = activeInvestments
            .GroupBy(i => i.Symbol.AssetType)
            .Select(g => new { AssetType = g.Key, TotalValue = g.Sum(i => i.CurrentValue.Amount) })
            .ToList();
        
        var largestAssetTypeValue = assetTypeGroups.Any() ? assetTypeGroups.Max(g => g.TotalValue) : 0;
        var concentrationRisk = totalValue.Amount > 0 ? largestAssetTypeValue / totalValue.Amount : 0;
        
        // Calculate diversification score (simplified)
        var diversificationScore = CalculateDiversificationScore(assetTypeCount, concentrationRisk);
        
        return await Task.FromResult(new PortfolioDiversificationAnalysis(
            assetTypeCount,
            sectorCount,
            concentrationRisk,
            diversificationScore));
    }
    
    /// <summary>
    /// Gets the top performing investments in a portfolio.
    /// </summary>
    /// <param name="portfolio">The portfolio to analyze</param>
    /// <param name="topCount">Number of top performers to return</param>
    /// <returns>Collection of top performing investments</returns>
    public async Task<IEnumerable<InvestmentPerformance>> GetTopPerformersAsync(Portfolio portfolio, int topCount = 5)
    {
        if (portfolio == null)
            throw new ArgumentNullException(nameof(portfolio));
        
        var performances = await GetInvestmentPerformancesAsync(portfolio);
        
        return performances
            .OrderByDescending(p => p.PercentageReturn)
            .Take(topCount);
    }
    
    /// <summary>
    /// Gets the worst performing investments in a portfolio.
    /// </summary>
    /// <param name="portfolio">The portfolio to analyze</param>
    /// <param name="bottomCount">Number of worst performers to return</param>
    /// <returns>Collection of worst performing investments</returns>
    public async Task<IEnumerable<InvestmentPerformance>> GetWorstPerformersAsync(Portfolio portfolio, int bottomCount = 5)
    {
        if (portfolio == null)
            throw new ArgumentNullException(nameof(portfolio));
        
        var performances = await GetInvestmentPerformancesAsync(portfolio);
        
        return performances
            .OrderBy(p => p.PercentageReturn)
            .Take(bottomCount);
    }
    
    /// <summary>
    /// Calculates portfolio risk metrics.
    /// </summary>
    /// <param name="portfolio">The portfolio to analyze</param>
    /// <returns>Risk analysis results</returns>
    public async Task<PortfolioRiskAnalysis> AnalyzeRiskAsync(Portfolio portfolio)
    {
        if (portfolio == null)
            throw new ArgumentNullException(nameof(portfolio));
        
        var activeInvestments = portfolio.Investments.Where(i => i.Status == InvestmentStatus.Active).ToList();
        
        if (!activeInvestments.Any())
        {
            return await Task.FromResult(new PortfolioRiskAnalysis(0, 0, 0, RiskLevel.VeryLow));
        }
        
        // Simplified risk calculation based on asset types and concentration
        var assetTypes = activeInvestments.Select(i => i.Symbol.AssetType).ToList();
        var riskScore = CalculateRiskScore(assetTypes);
        var riskLevel = DetermineRiskLevel(riskScore);
        
        // For now, we don't have historical data, so set volatility and max drawdown to 0
        var volatility = 0m;
        var maxDrawdown = 0m;
        
        return await Task.FromResult(new PortfolioRiskAnalysis(
            volatility,
            maxDrawdown,
            riskScore,
            riskLevel));
    }
    
    /// <summary>
    /// Gets performance data for all investments in the portfolio.
    /// </summary>
    /// <param name="portfolio">The portfolio to analyze</param>
    /// <returns>Collection of investment performance data</returns>
    private async Task<IEnumerable<InvestmentPerformance>> GetInvestmentPerformancesAsync(Portfolio portfolio)
    {
        var activeInvestments = portfolio.Investments.Where(i => i.Status == InvestmentStatus.Active);
        
        var performances = activeInvestments.Select(investment => new InvestmentPerformance(
            investment.Id,
            investment.Symbol,
            investment.CurrentValue,
            investment.GetTotalCost(),
            investment.GetUnrealizedGainLoss(),
            investment.GetPercentageGainLoss()));
        
        return await Task.FromResult(performances);
    }
    
    /// <summary>
    /// Calculates a diversification score based on asset type count and concentration risk.
    /// </summary>
    /// <param name="assetTypeCount">Number of different asset types</param>
    /// <param name="concentrationRisk">Concentration risk percentage</param>
    /// <returns>Diversification score (0-100)</returns>
    private static decimal CalculateDiversificationScore(int assetTypeCount, decimal concentrationRisk)
    {
        // Asset type diversity component (0-60 points)
        var assetTypeScore = Math.Min(assetTypeCount * 30, 60); // Increased limit to 60
        
        // Concentration risk component (0-50 points)
        var concentrationScore = Math.Max(0, 50 - (concentrationRisk * 100));
        
        return Math.Min(100, assetTypeScore + concentrationScore);
    }
    
    /// <summary>
    /// Calculates a risk score based on asset types in the portfolio.
    /// </summary>
    /// <param name="assetTypes">List of asset types in the portfolio</param>
    /// <returns>Risk score (0-100)</returns>
    private static decimal CalculateRiskScore(List<AssetType> assetTypes)
    {
        var riskWeights = new Dictionary<AssetType, decimal>
        {
            { AssetType.Stock, 30 },
            { AssetType.Crypto, 80 },
            { AssetType.Commodity, 40 },
            { AssetType.Option, 90 },
            { AssetType.Future, 85 },
            { AssetType.Bond, 10 },
            { AssetType.ETF, 20 },
            { AssetType.MutualFund, 15 },
            { AssetType.Forex, 60 }
        };
        
        if (!assetTypes.Any())
            return 0;
        
        var averageRisk = assetTypes.Average(at => riskWeights.GetValueOrDefault(at, 50));
        return Math.Min(100, averageRisk);
    }
    
    /// <summary>
    /// Determines the risk level based on the risk score.
    /// </summary>
    /// <param name="riskScore">The calculated risk score</param>
    /// <returns>The corresponding risk level</returns>
    private static RiskLevel DetermineRiskLevel(decimal riskScore)
    {
        return riskScore switch
        {
            < 20 => RiskLevel.VeryLow,
            < 25 => RiskLevel.Low, // Changed from < 35 to < 25
            < 60 => RiskLevel.Moderate,
            < 85 => RiskLevel.High,
            _ => RiskLevel.VeryHigh
        };
    }
    
    /// <summary>
    /// Processes an investment added event for portfolio valuation updates.
    /// </summary>
    /// <param name="investmentAddedEvent">The investment added event to process</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task ProcessEvent(InvestmentAddedEvent investmentAddedEvent)
    {
        // In a real application, this would:
        // 1. Load the portfolio from repository
        // 2. Recalculate portfolio metrics
        // 3. Update portfolio in repository
        // 4. Update any cached data
        // 5. Trigger additional events if needed
        
        await Task.CompletedTask;
        
        // For now, just simulate processing
        Console.WriteLine($"Processing investment added event: {investmentAddedEvent.Symbol.Ticker}");
    }
}
