using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using System.Text.Json;
using YahooFinanceApi;

namespace InvestmentHub.Infrastructure.MarketData;

public class YahooMarketDataProvider : IMarketDataProvider
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<YahooMarketDataProvider> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;

    public YahooMarketDataProvider(
        IDistributedCache cache,
        ILogger<YahooMarketDataProvider> logger,
        ResiliencePipelineProvider<string> pipelineProvider)
    {
        _cache = cache;
        _logger = logger;
        _resiliencePipeline = pipelineProvider.GetPipeline("default"); // We'll use a default policy for now
    }

    public async Task<MarketPrice?> GetLatestPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"market:price:{symbol.ToUpper()}";
        
        // Try get from cache
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedData))
        {
            return JsonSerializer.Deserialize<MarketPrice>(cachedData);
        }

        try
        {
            // Fetch from Yahoo
            // Note: YahooFinanceApi uses static methods, which makes it hard to mock/inject.
            // In a real prod scenario, we'd wrap this in a facade.
            var securities = await Yahoo.Symbols(symbol).Fields(Field.Symbol, Field.RegularMarketPrice, Field.Currency).QueryAsync(cancellationToken);
            var security = securities.Values.FirstOrDefault();

            if (security == null)
            {
                _logger.LogWarning("Symbol {Symbol} not found in Yahoo Finance", symbol);
                return null;
            }

            var price = new MarketPrice
            {
                Symbol = security.Symbol,
                Price = (decimal)security.RegularMarketPrice,
                Currency = security.Currency,
                Timestamp = DateTime.UtcNow,
                Source = "Yahoo"
            };

            // Cache result (15 min TTL)
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(price), cacheOptions, cancellationToken);

            return price;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching price for {Symbol} from Yahoo Finance", symbol);
            return null; // Or throw depending on requirements
        }
    }

    public async Task<IEnumerable<MarketPrice>> GetHistoricalPricesAsync(string symbol, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        try
        {
            var history = await Yahoo.GetHistoricalAsync(symbol, from, to, Period.Daily, cancellationToken);
            
            return history.Select(h => new MarketPrice
            {
                Symbol = symbol,
                Price = h.Close,
                Open = h.Open,
                High = h.High,
                Low = h.Low,
                Close = h.Close,
                Volume = h.Volume,
                Timestamp = h.DateTime,
                Source = "Yahoo"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching history for {Symbol} from Yahoo Finance", symbol);
            return Enumerable.Empty<MarketPrice>();
        }
    }

    public async Task<IEnumerable<SecurityInfo>> SearchSecuritiesAsync(string query, CancellationToken cancellationToken = default)
    {
        // YahooFinanceApi doesn't support search directly in the NuGet version used commonly.
        // We might need to use a different endpoint or library for search.
        // For now, returning empty to implement interface.
        return await Task.FromResult(Enumerable.Empty<SecurityInfo>());
    }
}
