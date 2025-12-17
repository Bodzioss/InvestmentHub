using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Interfaces;
using InvestmentHub.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.Services;

/// <summary>
/// Service for fetching market prices with daily caching.
/// Prevents redundant API calls by checking cache first.
/// </summary>
public class MarketPriceService
{
    private readonly IMarketDataProvider _marketDataProvider;
    private readonly IMarketPriceRepository _priceRepository;
    private readonly ILogger<MarketPriceService> _logger;

    public MarketPriceService(
        IMarketDataProvider marketDataProvider,
        IMarketPriceRepository priceRepository,
        ILogger<MarketPriceService> logger)
    {
        _marketDataProvider = marketDataProvider;
        _priceRepository = priceRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets current price for a symbol, using cached price if available today.
    /// </summary>
    public async Task<MarketPrice?> GetCurrentPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Check if we have today's price in cache
            var cachedPrice = await _priceRepository.GetTodaysPriceAsync(symbol, cancellationToken);
            if (cachedPrice != null)
            {
                _logger.LogDebug("Using cached price for {Symbol} from {FetchedAt}", symbol, cachedPrice.FetchedAt);
                return new MarketPrice
                {
                    Symbol = cachedPrice.Symbol,
                    Price = cachedPrice.Price,
                    Currency = cachedPrice.Currency,
                    Timestamp = cachedPrice.FetchedAt,
                    Source = cachedPrice.Source
                };
            }

            // 2. No cache hit, fetch from external provider
            _logger.LogInformation("No cached price for {Symbol}, fetching from provider", symbol);
            var livePrice = await _marketDataProvider.GetLatestPriceAsync(symbol, cancellationToken);

            if (livePrice == null)
            {
                _logger.LogWarning("Failed to fetch price for {Symbol} from provider", symbol);

                // Fallback: try to get most recent cached price (even if old)
                var fallbackPrice = await _priceRepository.GetLatestPriceAsync(symbol, cancellationToken);
                if (fallbackPrice != null)
                {
                    _logger.LogInformation("Using fallback cached price for {Symbol} from {FetchedAt}",
                        symbol, fallbackPrice.FetchedAt);
                    return new MarketPrice
                    {
                        Symbol = fallbackPrice.Symbol,
                        Price = fallbackPrice.Price,
                        Currency = fallbackPrice.Currency,
                        Timestamp = fallbackPrice.FetchedAt,
                        Source = fallbackPrice.Source
                    };
                }

                return null;
            }

            // 3. Save to cache for future use
            _logger.LogDebug("Caching price for {Symbol}: {Price} {Currency}",
                symbol, livePrice.Price, livePrice.Currency);

            await _priceRepository.SavePriceAsync(new CachedMarketPrice
            {
                Id = Guid.NewGuid(),
                Symbol = symbol,
                Price = livePrice.Price,
                Currency = livePrice.Currency,
                FetchedAt = DateTime.UtcNow,
                Source = livePrice.Source
            }, cancellationToken);

            return livePrice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching price for {Symbol}", symbol);

            // Try fallback to cached price
            var fallbackPrice = await _priceRepository.GetLatestPriceAsync(symbol, cancellationToken);
            if (fallbackPrice != null)
            {
                _logger.LogInformation("Using fallback cached price for {Symbol} after error", symbol);
                return new MarketPrice
                {
                    Symbol = fallbackPrice.Symbol,
                    Price = fallbackPrice.Price,
                    Currency = fallbackPrice.Currency,
                    Timestamp = fallbackPrice.FetchedAt,
                    Source = fallbackPrice.Source
                };
            }

            return null;
        }
    }

    /// <summary>
    /// Forces a fresh fetch from the provider, bypassing cache.
    /// Useful for manual refresh operations.
    /// Checks if today's price already exists to avoid duplicates.
    /// </summary>
    public async Task<MarketPrice?> ForceRefreshPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Force refreshing price for {Symbol}", symbol);

        // Check if we already have today's price to avoid duplicates
        var todaysPrice = await _priceRepository.GetTodaysPriceAsync(symbol, cancellationToken);
        if (todaysPrice != null)
        {
            _logger.LogDebug("Today's price already exists for {Symbol}, skipping duplicate fetch", symbol);
            return new MarketPrice
            {
                Symbol = todaysPrice.Symbol,
                Price = todaysPrice.Price,
                Currency = todaysPrice.Currency,
                Timestamp = todaysPrice.FetchedAt,
                Source = todaysPrice.Source
            };
        }

        var livePrice = await _marketDataProvider.GetLatestPriceAsync(symbol, cancellationToken);
        if (livePrice == null)
        {
            return null;
        }

        await _priceRepository.SavePriceAsync(new CachedMarketPrice
        {
            Id = Guid.NewGuid(),
            Symbol = symbol,
            Price = livePrice.Price,
            Currency = livePrice.Currency,
            FetchedAt = DateTime.UtcNow,
            Source = livePrice.Source
        }, cancellationToken);

        return livePrice;
    }

    /// <summary>
    /// Cleans up old cached prices older than the specified number of days.
    /// </summary>
    public async Task CleanupOldPricesAsync(int daysToKeep = 90, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        _logger.LogInformation("Cleaning up cached prices older than {CutoffDate}", cutoffDate);

        await _priceRepository.DeleteOldPricesAsync(cutoffDate, cancellationToken);
    }
}
