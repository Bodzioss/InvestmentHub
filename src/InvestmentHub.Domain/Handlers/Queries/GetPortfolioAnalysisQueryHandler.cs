using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Repositories;
using InvestmentHub.Domain.Services;
using MediatR;
using System.Linq;

namespace InvestmentHub.Domain.Handlers.Queries;

/// <summary>
/// Handler for GetPortfolioAnalysisQuery.
/// Responsible for performing comprehensive portfolio analysis with full business logic.
/// </summary>
public class GetPortfolioAnalysisQueryHandler : IRequestHandler<GetPortfolioAnalysisQuery, GetPortfolioAnalysisResult>
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IInvestmentRepository _investmentRepository;
    private readonly IPortfolioValuationService _valuationService;

    /// <summary>
    /// Initializes a new instance of the GetPortfolioAnalysisQueryHandler class.
    /// </summary>
    /// <param name="portfolioRepository">The portfolio repository</param>
    /// <param name="investmentRepository">The investment repository</param>
    /// <param name="valuationService">The portfolio valuation service</param>
    public GetPortfolioAnalysisQueryHandler(
        IPortfolioRepository portfolioRepository,
        IInvestmentRepository investmentRepository,
        IPortfolioValuationService valuationService)
    {
        _portfolioRepository = portfolioRepository ?? throw new ArgumentNullException(nameof(portfolioRepository));
        _investmentRepository = investmentRepository ?? throw new ArgumentNullException(nameof(investmentRepository));
        _valuationService = valuationService ?? throw new ArgumentNullException(nameof(valuationService));
    }

    /// <summary>
    /// Handles the GetPortfolioAnalysisQuery.
    /// </summary>
    /// <param name="request">The query request</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The result of the operation</returns>
    public async Task<GetPortfolioAnalysisResult> Handle(GetPortfolioAnalysisQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Check for cancellation
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("Cancellation was requested");
            }

            // 1. Load portfolio
            var portfolio = await _portfolioRepository.GetByIdAsync(request.PortfolioId, cancellationToken);
            if (portfolio == null)
            {
                return GetPortfolioAnalysisResult.Failure("Portfolio not found");
            }

            // 2. Load investments for the portfolio
            var investments = await _investmentRepository.GetByPortfolioIdAsync(request.PortfolioId, cancellationToken);
            
            // 3. Add investments to portfolio (if not already loaded)
            foreach (var investment in investments)
            {
                if (!portfolio.Investments.Any(i => i.Id == investment.Id))
                {
                    portfolio.AddInvestment(investment);
                }
            }

            // 4. Perform risk analysis
            var riskAnalysis = await _valuationService.AnalyzeRiskAsync(portfolio);

            // 5. Perform diversification analysis
            var diversificationAnalysis = await _valuationService.AnalyzeDiversificationAsync(portfolio);

            // 6. Calculate basic metrics
            var totalValue = portfolio.GetTotalValue();
            var totalCost = portfolio.GetTotalCost();
            var unrealizedGainLoss = portfolio.GetTotalGainLoss();
            var percentageReturn = portfolio.GetPercentageGainLoss();

            // 7. Prepare detailed breakdown if requested
            IReadOnlyList<AssetTypeBreakdown>? assetTypeBreakdown = null;
            IReadOnlyList<InvestmentHub.Domain.Queries.InvestmentPerformance>? topPerformers = null;
            IReadOnlyList<InvestmentHub.Domain.Queries.InvestmentPerformance>? worstPerformers = null;

            if (request.IncludeDetailedBreakdown)
            {
                assetTypeBreakdown = CalculateAssetTypeBreakdown(portfolio, totalValue);
                var performances = CalculateInvestmentPerformances(portfolio);
                topPerformers = performances.OrderByDescending(p => p.PercentageReturn).Take(5).ToList();
                worstPerformers = performances.OrderBy(p => p.PercentageReturn).Take(5).ToList();
            }

            // 8. Create analysis data
            var analysisData = new PortfolioAnalysisData(
                request.PortfolioId,
                DateTime.UtcNow,
                totalValue,
                totalCost,
                unrealizedGainLoss,
                percentageReturn,
                portfolio.Investments.Count,
                diversificationAnalysis.AssetTypeCount,
                diversificationAnalysis.ConcentrationRisk,
                diversificationAnalysis.DiversificationScore,
                riskAnalysis.RiskScore,
                riskAnalysis.RiskLevel,
                assetTypeBreakdown,
                topPerformers,
                worstPerformers);

            // 9. Return success
            return GetPortfolioAnalysisResult.Success(analysisData);
        }
        catch (OperationCanceledException)
        {
            // Re-throw cancellation exceptions
            throw;
        }
        catch (Exception ex)
        {
            return GetPortfolioAnalysisResult.Failure($"Failed to analyze portfolio: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculates asset type breakdown for the portfolio.
    /// </summary>
    /// <param name="portfolio">The portfolio</param>
    /// <param name="totalValue">The total portfolio value</param>
    /// <returns>Asset type breakdown</returns>
    private static IReadOnlyList<AssetTypeBreakdown> CalculateAssetTypeBreakdown(Portfolio portfolio, Money totalValue)
    {
        var breakdown = portfolio.Investments
            .GroupBy(i => i.Symbol.AssetType)
            .Select(g => new AssetTypeBreakdown(
                g.Key,
                g.Count(),
                new Money(g.Sum(i => i.CurrentValue.Amount), totalValue.Currency),
                totalValue.Amount > 0 ? g.Sum(i => i.CurrentValue.Amount) / totalValue.Amount : 0))
            .OrderByDescending(b => b.TotalValue.Amount)
            .ToList();

        return breakdown;
    }

    /// <summary>
    /// Calculates performance data for all investments in the portfolio.
    /// </summary>
    /// <param name="portfolio">The portfolio</param>
    /// <returns>Investment performance data</returns>
    private static IReadOnlyList<InvestmentHub.Domain.Queries.InvestmentPerformance> CalculateInvestmentPerformances(Portfolio portfolio)
    {
        var performances = portfolio.Investments
            .Select(i => new InvestmentHub.Domain.Queries.InvestmentPerformance(
                i.Id,
                i.Symbol,
                i.GetPercentageGainLoss(),
                i.GetUnrealizedGainLoss()))
            .ToList();

        return performances;
    }
}
