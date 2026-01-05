using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Interfaces;
using InvestmentHub.Domain.Repositories;
using InvestmentHub.Infrastructure.MarketData;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.Services;

/// <summary>
/// Service for fetching market prices with daily caching.
/// Prevents redundant API calls by checking cache first.
/// </summary>
public class MarketPriceService
{
    private readonly IEnumerable<IMarketDataProvider> _marketDataProviders;
    private readonly IMarketPriceRepository _priceRepository;
    private readonly ILogger<MarketPriceService> _logger;

    public MarketPriceService(
        IEnumerable<IMarketDataProvider> marketDataProviders,
        IMarketPriceRepository priceRepository,
        ILogger<MarketPriceService> logger)
    {
        _marketDataProviders = marketDataProviders;
        _priceRepository = priceRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets current price for a symbol, using cached price if available today.
    /// </summary>
    public async Task<MarketPrice?> GetCurrentPriceAsync(Domain.ValueObjects.Symbol symbol, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Check if we have today's price in cache
            var cachedPrice = await _priceRepository.GetTodaysPriceAsync(symbol.Ticker, cancellationToken);
            if (cachedPrice != null)
            {
                _logger.LogDebug("Using cached price for {Symbol} from {FetchedAt}", symbol.Ticker, cachedPrice.FetchedAt);
                return new MarketPrice
                {
                    Symbol = cachedPrice.Symbol,
                    Price = cachedPrice.Price,
                    Currency = cachedPrice.Currency,
                    Timestamp = cachedPrice.FetchedAt,
                    Source = cachedPrice.Source
                };
            }

            // 2. No cache hit, fetch from external providers
            _logger.LogInformation("No cached price for {Symbol}, fetching from providers", symbol.Ticker);

            MarketPrice? livePrice = null;

            // Prioritize providers: Stooq first for Polish/Catalyst, Yahoo for others
            var orderedProviders = _marketDataProviders.OrderBy(p =>
                p is StooqMarketDataProvider && (symbol.Exchange == "GPW" || symbol.Exchange == "Catalyst" || symbol.Exchange == "NewConnect" || symbol.Exchange == "WSE" || symbol.Exchange == "WAR") ? 0 : 1);

            foreach (var provider in orderedProviders)
            {
                livePrice = await provider.GetLatestPriceAsync(symbol, cancellationToken);
                if (livePrice != null) break;
            }

            if (livePrice == null)
            {
                _logger.LogWarning("Failed to fetch price for {Symbol} from any provider", symbol.Ticker);

                // Fallback: try to get most recent cached price (even if old)
                var fallbackPrice = await _priceRepository.GetLatestPriceAsync(symbol.Ticker, cancellationToken);
                if (fallbackPrice != null)
                {
                    _logger.LogInformation("Using fallback cached price for {Symbol} from {FetchedAt}",
                        symbol.Ticker, fallbackPrice.FetchedAt);
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
                symbol.Ticker, livePrice.Price, livePrice.Currency);

            await _priceRepository.SavePriceAsync(new CachedMarketPrice
            {
                Id = Guid.NewGuid(),
                Symbol = symbol.Ticker,
                Price = livePrice.Price,
                Currency = livePrice.Currency,
                FetchedAt = DateTime.UtcNow,
                Source = livePrice.Source
            }, cancellationToken);

            return livePrice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching price for {Symbol}", symbol.Ticker);

            // Try fallback to cached price
            var fallbackPrice = await _priceRepository.GetLatestPriceAsync(symbol.Ticker, cancellationToken);
            if (fallbackPrice != null)
            {
                _logger.LogInformation("Using fallback cached price for {Symbol} after error", symbol.Ticker);
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
    public async Task<MarketPrice?> ForceRefreshPriceAsync(Domain.ValueObjects.Symbol symbol, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Force refreshing price for {Symbol}", symbol.Ticker);

        // FORCE REFRESH - bypasses cache for ALL instruments!
        // Always fetches from external provider, regardless of whether today's data exists.
        // Provider prioritization: Stooq first for Polish exchanges, Yahoo for others
        var orderedProviders = _marketDataProviders.OrderBy(p =>
            p is StooqMarketDataProvider &&
            (symbol.Exchange == "GPW" || symbol.Exchange == "Catalyst" ||
             symbol.Exchange == "NewConnect" || symbol.Exchange == "WSE" || symbol.Exchange == "WAR")
            ? 0 : 1);

        MarketPrice? livePrice = null;
        foreach (var provider in orderedProviders)
        {
            livePrice = await provider.GetLatestPriceAsync(symbol, cancellationToken);
            if (livePrice != null) break;
        }

        if (livePrice == null)
        {
            return null;
        }

        await _priceRepository.SavePriceAsync(new CachedMarketPrice
        {
            Id = Guid.NewGuid(),
            Symbol = symbol.Ticker,
            Price = livePrice.Price,
            Currency = livePrice.Currency,
            FetchedAt = DateTime.UtcNow,
            Source = livePrice.Source
        }, cancellationToken);

        return livePrice;
    }

    /// <summary>
    /// Gets historical prices for a symbol.
    /// tries all providers until one returns data (Yahoo is prioritized for history).
    /// </summary>
    public async Task<IEnumerable<MarketPrice>> GetHistoricalPricesAsync(Domain.ValueObjects.Symbol symbol, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        // Try all providers until one returns data. 
        // Yahoo is currently the only one with full historical support in this implementation.
        var orderedProviders = _marketDataProviders.OrderBy(p => p is YahooMarketDataProvider ? 0 : 1);

        foreach (var provider in orderedProviders)
        {
            try
            {
                var history = await provider.GetHistoricalPricesAsync(symbol, from, to, cancellationToken);
                var historyList = history.ToList();
                if (historyList.Any()) return historyList;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error fetching historical prices for {Symbol} from {Provider}", symbol.Ticker, provider.GetType().Name);
            }
        }

        return Enumerable.Empty<MarketPrice>();
    }

    /// <summary>
    /// Forces a fresh fetch from the provider and returns detailed logs of the process.
    /// </summary>
    public async Task<(MarketPrice? Price, List<string> Logs)> ForceRefreshPriceWithLogsAsync(Domain.ValueObjects.Symbol symbol, CancellationToken cancellationToken = default)
    {
        var logs = new List<string>();
        logs.Add($"[{DateTime.Now:T}] Starting force refresh for {symbol.Ticker} ({symbol.Exchange}, {symbol.AssetType})");

        try
        {
            MarketPrice? livePrice = null;

            // Prioritize providers: Stooq first for Polish/Catalyst, Yahoo for others
            var orderedProviders = _marketDataProviders.OrderBy(p =>
                p is StooqMarketDataProvider && (symbol.Exchange == "GPW" || symbol.Exchange == "Catalyst" || symbol.Exchange == "NewConnect" || symbol.Exchange == "WSE" || symbol.Exchange == "WAR") ? 0 : 1);

            foreach (var provider in orderedProviders)
            {
                var providerName = provider.GetType().Name;
                logs.Add($"[{DateTime.Now:T}] Trying provider: {providerName}");

                try
                {
                    livePrice = await provider.GetLatestPriceAsync(symbol, cancellationToken, logs);
                    if (livePrice != null)
                    {
                        logs.Add($"[{DateTime.Now:T}] Success from {providerName}: {livePrice.Price} {livePrice.Currency}");
                        break;
                    }
                    else
                    {
                        logs.Add($"[{DateTime.Now:T}] Provider {providerName} returned no data.");
                    }
                }
                catch (Exception ex)
                {
                    logs.Add($"[{DateTime.Now:T}] ERROR from {providerName}: {ex.Message}");
                }
            }

            if (livePrice != null)
            {
                logs.Add($"[{DateTime.Now:T}] Updating cache with fresh price.");
                await _priceRepository.SavePriceAsync(new CachedMarketPrice
                {
                    Id = Guid.NewGuid(),
                    Symbol = symbol.Ticker,
                    Price = livePrice.Price,
                    Currency = livePrice.Currency,
                    FetchedAt = DateTime.UtcNow,
                    Source = livePrice.Source
                }, cancellationToken);

                return (livePrice, logs);
            }

            logs.Add($"[{DateTime.Now:T}] Failed to fetch price from any provider.");
            return (null, logs);
        }
        catch (Exception ex)
        {
            logs.Add($"[{DateTime.Now:T}] CRITICAL ERROR: {ex.Message}");
            return (null, logs);
        }
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
