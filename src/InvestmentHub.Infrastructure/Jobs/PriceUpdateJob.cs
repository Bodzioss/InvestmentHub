using InvestmentHub.Domain.Repositories;
using InvestmentHub.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.Jobs;

public class PriceUpdateJob
{
    private readonly IInvestmentRepository _investmentRepository;
    private readonly MarketPriceService _marketPriceService;
    private readonly ILogger<PriceUpdateJob> _logger;

    public PriceUpdateJob(
        IInvestmentRepository investmentRepository,
        MarketPriceService marketPriceService,
        ILogger<PriceUpdateJob> logger)
    {
        _investmentRepository = investmentRepository;
        _marketPriceService = marketPriceService;
        _logger = logger;
    }

    public async Task UpdateActivePricesAsync()
    {
        _logger.LogInformation("Starting PriceUpdateJob...");

        // 1. Get all unique symbols from active investments
        var investments = await _investmentRepository.GetAllAsync(CancellationToken.None);
        var tickers = investments.Select(i => i.Symbol.Ticker).Distinct().ToList();

        _logger.LogInformation("Found {Count} unique symbols to update", tickers.Count);

        foreach (var ticker in tickers)
        {
            try
            {
                // 2. Fetch price using smart caching service
                // This will use cached price if already fetched today
                var price = await _marketPriceService.GetCurrentPriceAsync(ticker, CancellationToken.None);

                if (price != null)
                {
                    _logger.LogDebug("Processed price for {Symbol}: {Price} {Currency}",
                        ticker, price.Price, price.Currency);
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
