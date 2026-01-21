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

public class YahooMarketDataProvider(
    IDistributedCache cache,
    ILogger<YahooMarketDataProvider> logger,
    ResiliencePipelineProvider<string> pipelineProvider,
    YahooQuotes yahooQuotes)
    : IMarketDataProvider
{
    private readonly IDistributedCache _cache = cache;
    private readonly ILogger<YahooMarketDataProvider> _logger = logger;
    private readonly ResiliencePipeline _pipeline = pipelineProvider.GetPipeline("default");
    private readonly YahooQuotes _yahooQuotes = yahooQuotes;

    public async Task<MarketPrice?> GetLatestPriceAsync(Domain.ValueObjects.Symbol symbol, CancellationToken cancellationToken = default, List<string>? traceLogs = null)
    {
        var ticker = symbol.Ticker;
        if ((symbol.Exchange == "GPW" || symbol.Exchange == "WSE" || symbol.Exchange == "WAR" || symbol.Exchange == "NewConnect" || symbol.Exchange == "Catalyst") &&
            !ticker.EndsWith(".WA") &&
            !ticker.Contains('.'))
        {
            ticker += ".WA";
            traceLogs?.Add($"Yahoo: Polish exchange detected ({symbol.Exchange}), added .WA suffix -> {ticker}");
        }

        var cacheKey = $"market:price:v2:{ticker.ToUpper()}";

        // Try get from cache
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedData))
        {
            return JsonSerializer.Deserialize<MarketPrice>(cachedData);
        }

        try
        {
            traceLogs?.Add($"Yahoo: Fetching snapshot for {ticker}...");
            var security = await _pipeline.ExecuteAsync(async token =>
                await _yahooQuotes.GetSnapshotAsync(ticker, token), cancellationToken);

            if (security == null)
            {
                _logger.LogWarning("Symbol {Ticker} ({Exchange}) - Bond not found in Yahoo Finance", ticker, symbol.Exchange);
                traceLogs?.Add($"Yahoo ERROR: Symbol {ticker} ({symbol.Exchange}) - Bond not found in Yahoo Finance");
                return null;
            }

            // RegularMarketPrice seems to be decimal (not nullable) based on error
            // But we should check if it's 0 or valid? 
            // Stooq doesn't explicitly return currency in this simplified API. 
            // We'll infer it: PLN for GPW-like tickers, or use a default.
            // Improved version could check if ticker is USDPLN etc.
            var price = new MarketPrice
            {
                Symbol = symbol.Ticker,
                Price = security.RegularMarketPrice,
                Currency = security.Currency.ToString() ?? "PLN",
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
            _logger.LogError(ex, "Error fetching price for {Symbol} from Yahoo Finance", symbol.Ticker);
            return null;
        }
    }

    public async Task<IEnumerable<MarketPrice>> GetHistoricalPricesAsync(Domain.ValueObjects.Symbol symbol, DateTime from, DateTime to, CancellationToken cancellationToken = default, List<string>? traceLogs = null)
    {
        var ticker = symbol.Ticker;
        if ((symbol.Exchange == "GPW" || symbol.Exchange == "WSE" || symbol.Exchange == "WAR" || symbol.Exchange == "NewConnect" || symbol.Exchange == "Catalyst") && !ticker.EndsWith(".WA"))
        {
            ticker += ".WA";
        }

        try
        {
            // Based on YahooQuotesApi source, GetHistoryAsync takes (symbol, baseSymbol, ct)
            // It returns Result<History>
            var result = await _yahooQuotes.GetHistoryAsync(ticker, "", cancellationToken);

            if (result.HasError)
            {
                _logger.LogWarning("YahooQuotesApi error for {Symbol}: {Error}", symbol, result.Error);
                return [];
            }

            var history = result.Value;

            return history.Ticks.Select(h => new MarketPrice
            {
                Symbol = symbol.Ticker,
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
            _logger.LogError(ex, "Error fetching history for {Symbol} from Yahoo Finance via YahooQuotesApi", symbol.Ticker);
            return [];
        }
    }

    public Task<IEnumerable<SecurityInfo>> SearchSecuritiesAsync(string query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Enumerable.Empty<SecurityInfo>());
    }
}
