using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.ReadModels;
using InvestmentHub.Domain.ValueObjects;
using Marten;
using MediatR;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Domain.Handlers.Queries;

/// <summary>
/// Handler for GetPortfolioAnalysisQuery.
/// Responsible for performing comprehensive portfolio analysis from read models using Marten.
/// </summary>
public class GetPortfolioAnalysisQueryHandler : IRequestHandler<GetPortfolioAnalysisQuery, GetPortfolioAnalysisResult>
{
    private readonly IDocumentSession _session;
    private readonly ILogger<GetPortfolioAnalysisQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the GetPortfolioAnalysisQueryHandler class.
    /// </summary>
    /// <param name="session">The Marten document session</param>
    /// <param name="logger">The logger</param>
    public GetPortfolioAnalysisQueryHandler(
        IDocumentSession session,
        ILogger<GetPortfolioAnalysisQueryHandler> logger)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the GetPortfolioAnalysisQuery by reading from read models.
    /// </summary>
    /// <param name="request">The query request</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The result of the operation</returns>
    public async Task<GetPortfolioAnalysisResult> Handle(GetPortfolioAnalysisQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Performing portfolio analysis for {PortfolioId}", request.PortfolioId.Value);

            cancellationToken.ThrowIfCancellationRequested();

            // 1. Load portfolio read model
            var portfolio = await _session.LoadAsync<PortfolioReadModel>(request.PortfolioId.Value, cancellationToken);
            if (portfolio == null)
            {
                _logger.LogWarning("Portfolio {PortfolioId} not found", request.PortfolioId.Value);
                return GetPortfolioAnalysisResult.Failure("Portfolio not found");
            }

            // 2. Load all investments for the portfolio
            var investments = await _session.Query<InvestmentReadModel>()
                .Where(i => i.PortfolioId == request.PortfolioId.Value)
                .ToListAsync(cancellationToken);

            if (!investments.Any())
            {
                _logger.LogInformation("No investments found for portfolio {PortfolioId}", request.PortfolioId.Value);
                // Return empty analysis
                return CreateEmptyAnalysis(request.PortfolioId, Enum.Parse<Currency>(portfolio.Currency));
            }

            // 3. Calculate basic metrics from read models
            var currency = Enum.Parse<Currency>(portfolio.Currency);
            var totalValue = new Money(portfolio.TotalValue, currency);
            var totalCost = new Money(investments.Sum(i => i.TotalCost), currency);
            var unrealizedGainLoss = new Money(investments.Sum(i => i.UnrealizedProfitLoss), currency);
            var percentageReturn = totalCost.Amount != 0 
                ? (unrealizedGainLoss.Amount / totalCost.Amount) * 100 
                : 0;

            // 4. Perform simple risk and diversification analysis
            var assetTypeCount = investments.Select(i => i.AssetType).Distinct().Count();
            var concentrationRisk = CalculateConcentrationRisk(investments, totalValue.Amount);
            var diversificationScore = CalculateDiversificationScore(assetTypeCount, investments.Count, concentrationRisk);
            var riskScore = CalculateRiskScore(investments, percentageReturn);
            var riskLevel = DetermineRiskLevel(riskScore);

            // 5. Prepare detailed breakdown if requested
            IReadOnlyList<AssetTypeBreakdown>? assetTypeBreakdown = null;
            IReadOnlyList<InvestmentHub.Domain.Queries.InvestmentPerformance>? topPerformers = null;
            IReadOnlyList<InvestmentHub.Domain.Queries.InvestmentPerformance>? worstPerformers = null;

            if (request.IncludeDetailedBreakdown)
            {
                assetTypeBreakdown = CalculateAssetTypeBreakdown(investments, totalValue);
                var performances = CalculateInvestmentPerformances(investments, currency);
                topPerformers = performances.OrderByDescending(p => p.PercentageReturn).Take(5).ToList();
                worstPerformers = performances.OrderBy(p => p.PercentageReturn).Take(5).ToList();
            }

            // 6. Create analysis data
            var analysisData = new PortfolioAnalysisData(
                request.PortfolioId,
                DateTime.UtcNow,
                totalValue,
                totalCost,
                unrealizedGainLoss,
                percentageReturn,
                portfolio.InvestmentCount,
                assetTypeCount,
                concentrationRisk,
                diversificationScore,
                riskScore,
                riskLevel,
                assetTypeBreakdown,
                topPerformers,
                worstPerformers);

            _logger.LogInformation("Successfully completed portfolio analysis for {PortfolioId}", request.PortfolioId.Value);

            return GetPortfolioAnalysisResult.Success(analysisData);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Portfolio analysis cancelled for {PortfolioId}", request.PortfolioId.Value);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze portfolio {PortfolioId}: {Message}", 
                request.PortfolioId.Value, ex.Message);
            return GetPortfolioAnalysisResult.Failure($"Failed to analyze portfolio: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates an empty analysis result for portfolios with no investments.
    /// </summary>
    private static GetPortfolioAnalysisResult CreateEmptyAnalysis(PortfolioId portfolioId, Currency currency)
    {
        var zero = new Money(0, currency);
        var analysisData = new PortfolioAnalysisData(
            portfolioId,
            DateTime.UtcNow,
            zero,
            zero,
            zero,
            0m,
            0,
            0,
            0m,
            0m,
            0m,
            RiskLevel.Low,
            null,
            null,
            null);

        return GetPortfolioAnalysisResult.Success(analysisData);
    }

    /// <summary>
    /// Calculates asset type breakdown for the portfolio.
    /// </summary>
    /// <param name="investments">The investments</param>
    /// <param name="totalValue">The total portfolio value</param>
    /// <returns>Asset type breakdown</returns>
    private static IReadOnlyList<AssetTypeBreakdown> CalculateAssetTypeBreakdown(
        IReadOnlyList<InvestmentReadModel> investments, 
        Money totalValue)
    {
        var breakdown = investments
            .GroupBy(i => i.AssetType)
            .Select(g => new AssetTypeBreakdown(
                Enum.Parse<AssetType>(g.Key),
                g.Count(),
                new Money(g.Sum(i => i.CurrentValue), totalValue.Currency),
                totalValue.Amount > 0 ? g.Sum(i => i.CurrentValue) / totalValue.Amount : 0))
            .OrderByDescending(b => b.TotalValue.Amount)
            .ToList();

        return breakdown;
    }

    /// <summary>
    /// Calculates performance data for all investments from read models.
    /// </summary>
    /// <param name="investments">The investment read models</param>
    /// <param name="currency">The portfolio currency</param>
    /// <returns>Investment performance data</returns>
    private static IReadOnlyList<InvestmentHub.Domain.Queries.InvestmentPerformance> CalculateInvestmentPerformances(
        IReadOnlyList<InvestmentReadModel> investments,
        Currency currency)
    {
        var performances = investments
            .Select(i => new InvestmentHub.Domain.Queries.InvestmentPerformance(
                new InvestmentId(i.Id),
                new Symbol(i.Ticker, i.Exchange, Enum.Parse<AssetType>(i.AssetType)),
                i.ROIPercentage,
                new Money(i.UnrealizedProfitLoss, currency)))
            .ToList();

        return performances;
    }

    /// <summary>
    /// Calculates concentration risk based on largest investment percentage.
    /// </summary>
    private static decimal CalculateConcentrationRisk(IReadOnlyList<InvestmentReadModel> investments, decimal totalValue)
    {
        if (totalValue == 0 || !investments.Any())
            return 0m;

        var largestInvestmentValue = investments.Max(i => i.CurrentValue);
        return largestInvestmentValue / totalValue;
    }

    /// <summary>
    /// Calculates diversification score (0-100) based on asset types, count, and concentration.
    /// </summary>
    private static decimal CalculateDiversificationScore(int assetTypeCount, int investmentCount, decimal concentrationRisk)
    {
        if (investmentCount == 0)
            return 0m;

        // Score components:
        // - Asset type diversity (max 40 points): more asset types = better
        // - Investment count (max 30 points): more investments = better
        // - Concentration risk (max 30 points): lower concentration = better

        var assetTypeScore = Math.Min(assetTypeCount * 10m, 40m);
        var countScore = Math.Min(investmentCount * 3m, 30m);
        var concentrationScore = Math.Max(0, 30m - (concentrationRisk * 100m));

        return assetTypeScore + countScore + concentrationScore;
    }

    /// <summary>
    /// Calculates risk score (0-100) based on volatility and return metrics.
    /// Simplified version without historical data.
    /// </summary>
    private static decimal CalculateRiskScore(IReadOnlyList<InvestmentReadModel> investments, decimal percentageReturn)
    {
        if (!investments.Any())
            return 0m;

        // Simplified risk calculation based on:
        // - Investment count (fewer = higher risk)
        // - ROI variance (higher variance = higher risk)
        // - Overall return volatility

        var avgROI = investments.Average(i => i.ROIPercentage);
        var roiVariance = investments.Any() 
            ? investments.Sum(i => Math.Pow((double)(i.ROIPercentage - avgROI), 2)) / investments.Count
            : 0;

        var riskFromVariance = Math.Min((decimal)Math.Sqrt(roiVariance), 50m);
        var riskFromCount = Math.Max(0, 25m - (investments.Count * 2m));
        var riskFromReturn = Math.Abs(percentageReturn) > 50 ? 25m : Math.Abs(percentageReturn) / 2m;

        return Math.Min(100m, riskFromVariance + riskFromCount + riskFromReturn);
    }

    /// <summary>
    /// Determines risk level enum from risk score.
    /// </summary>
    private static RiskLevel DetermineRiskLevel(decimal riskScore)
    {
        return riskScore switch
        {
            < 20m => RiskLevel.VeryLow,
            < 40m => RiskLevel.Low,
            < 60m => RiskLevel.Moderate,
            < 80m => RiskLevel.High,
            _ => RiskLevel.VeryHigh
        };
    }
}
