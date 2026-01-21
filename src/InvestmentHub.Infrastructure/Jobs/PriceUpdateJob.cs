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
        var symbols = investments.Select(i => i.Symbol)
            .GroupBy(s => new { s.Ticker, s.Exchange })
            .Select(g => g.First())
            .ToList();

        _logger.LogInformation("Found {Count} unique symbols to update", symbols.Count);

        foreach (var symbol in symbols)
        {
            try
            {
                // 2. Fetch price using smart caching service
                // This will use cached price if already fetched today
                var price = await _marketPriceService.GetCurrentPriceAsync(symbol, CancellationToken.None);

                if (price != null)
                {
                    _logger.LogDebug("Processed price for {Symbol}: {Price} {Currency}",
                        symbol.Ticker, price.Price, price.Currency);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update price for {Symbol}", symbol.Ticker);
            }
        }

        _logger.LogInformation("PriceUpdateJob completed successfully");
    }
}
