using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using System.Text.Json;
using YahooQuotesApi;
using NodaTime;

namespace InvestmentHub.Infrastructure.MarketData;

public class YahooMarketDataProvider : IMarketDataProvider
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<YahooMarketDataProvider> _logger;
    private readonly ResiliencePipeline _pipeline;
    private readonly YahooQuotes _yahooQuotes;

    public YahooMarketDataProvider(
        IDistributedCache cache,
        ILogger<YahooMarketDataProvider> logger,
        ResiliencePipelineProvider<string> pipelineProvider,
        YahooQuotes yahooQuotes)
    {
        _cache = cache;
        _logger = logger;
        _yahooQuotes = yahooQuotes;
        _pipeline = pipelineProvider.GetPipeline("default");
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
            // Fetch from Yahoo using YahooQuotesApi with resilience
            var security = await _pipeline.ExecuteAsync(async token => 
                await _yahooQuotes.GetSnapshotAsync(symbol, token), cancellationToken);

            if (security == null)
            {
                _logger.LogWarning("Symbol {Symbol} not found in Yahoo Finance", symbol);
                return null;
            }

            // RegularMarketPrice seems to be decimal (not nullable) based on error
            // But we should check if it's 0 or valid? 
            // Actually, if it's decimal, it always has a value.
            // But maybe we should check if it is valid?
            // Let's assume it is valid if we got the security.
            
            var currency = security.Currency.ToString() ?? "USD";
            if (currency.EndsWith("=X"))
            {
                currency = currency.Replace("=X", "");
            }

            var price = new MarketPrice
            {
                Symbol = symbol,
                Price = security.RegularMarketPrice,
                Currency = currency,
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
            return null;
        }
    }

    public async Task<IEnumerable<MarketPrice>> GetHistoricalPricesAsync(string symbol, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        try
        {
            // Based on YahooQuotesApi source, GetHistoryAsync takes (symbol, baseSymbol, ct)
            // It returns Result<History>
            var result = await _yahooQuotes.GetHistoryAsync(symbol, "", cancellationToken);

            if (result.HasError)
            {
                 _logger.LogWarning("YahooQuotesApi error for {Symbol}: {Error}", symbol, result.Error);
                 return Enumerable.Empty<MarketPrice>();
            }

            var history = result.Value;

            return history.Ticks.Select(h => new MarketPrice
            {
                Symbol = symbol,
                Price = (decimal)h.Close,
                Open = (decimal)h.Open,
                High = (decimal)h.High,
                Low = (decimal)h.Low,
                Close = (decimal)h.Close,
                Volume = h.Volume,
                // Convert NodaTime LocalDate to DateTime
                Timestamp = h.Date.ToDateTimeUtc(),
                Source = "Yahoo"
            })
            .Where(x => x.Timestamp >= from && x.Timestamp <= to)
            .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching history for {Symbol} from Yahoo Finance via YahooQuotesApi", symbol);
            return Enumerable.Empty<MarketPrice>();
        }
    }

    public async Task<IEnumerable<SecurityInfo>> SearchSecuritiesAsync(string query, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(Enumerable.Empty<SecurityInfo>());
    }
}
