using InvestmentHub.Domain.Interfaces;
using InvestmentHub.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.Jobs;

public class PriceUpdateJob
{
    private readonly IInvestmentRepository _investmentRepository;
    private readonly IMarketDataProvider _marketDataProvider;
    private readonly ILogger<PriceUpdateJob> _logger;

    public PriceUpdateJob(
        IInvestmentRepository investmentRepository,
        IMarketDataProvider marketDataProvider,
        ILogger<PriceUpdateJob> logger)
    {
        _investmentRepository = investmentRepository;
        _marketDataProvider = marketDataProvider;
        _logger = logger;
    }

    public async Task UpdateActivePricesAsync()
    {
        _logger.LogInformation("Starting PriceUpdateJob...");

        // 1. Get all unique symbols from active investments
        // Note: In a real app, we might want a dedicated read model or query for this
        // to avoid loading all investments. For now, we'll assume the repo has a method
        // or we fetch all and filter (not ideal for large datasets).
        // Let's assume we need to add GetActiveSymbolsAsync to IInvestmentRepository first.
        // For this iteration, I'll use a placeholder logic or we need to extend the repo.
        
        // Placeholder: Fetching all investments and getting distinct symbols
        // This is a temporary solution until we optimize the repository
        var investments = await _investmentRepository.GetAllAsync(CancellationToken.None);
        var tickers = investments.Select(i => i.Symbol.Ticker).Distinct().ToList();

        _logger.LogInformation("Found {Count} unique symbols to update", tickers.Count);

        foreach (var ticker in tickers)
        {
            try
            {
                // 2. Fetch latest price (this will automatically cache it in Redis via the provider)
                var price = await _marketDataProvider.GetLatestPriceAsync(ticker, CancellationToken.None);
                
                if (price != null)
                {
                    _logger.LogDebug("Updated price for {Symbol}: {Price} {Currency}", ticker, price.Price, price.Currency);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update price for {Symbol}", ticker);
            }
        }

        _logger.LogInformation("PriceUpdateJob completed successfully");
    }
}
