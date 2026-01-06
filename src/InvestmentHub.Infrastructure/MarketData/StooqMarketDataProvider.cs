using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Interfaces;
using InvestmentHub.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using System.Globalization;

namespace InvestmentHub.Infrastructure.MarketData;

/// <summary>
/// Market data provider using Stooq.pl CSV API.
/// Useful for Polish stocks and Catalyst bonds.
/// </summary>
public class StooqMarketDataProvider : IMarketDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StooqMarketDataProvider> _logger;
    private readonly ResiliencePipeline _pipeline;

    public StooqMarketDataProvider(
        HttpClient httpClient,
        ILogger<StooqMarketDataProvider> logger,
        ResiliencePipelineProvider<string> pipelineProvider)
    {
        _httpClient = httpClient;
        _logger = logger;
        _pipeline = pipelineProvider.GetPipeline("default");
    }

    public async Task<MarketPrice?> GetLatestPriceAsync(Domain.ValueObjects.Symbol symbol, CancellationToken cancellationToken = default, List<string>? traceLogs = null)
    {
        var ticker = symbol.Ticker;
        var isPolishExchange = symbol.Exchange == "GPW" || symbol.Exchange == "WSE" || symbol.Exchange == "WAR" || symbol.Exchange == "NewConnect" || symbol.Exchange == "Catalyst";

        // Step 1: Try the raw ticker first
        var price = await FetchPriceAsync(ticker, symbol, cancellationToken, traceLogs);

        // Step 2: If it failed with "B/D" and it's a Polish exchange, try with .PL suffix
        if (price == null && isPolishExchange && !ticker.EndsWith(".PL", StringComparison.OrdinalIgnoreCase))
        {
            var suffixedTicker = $"{ticker}.PL";
            traceLogs?.Add($"Stooq: Raw ticker {ticker} returned no data, retrying with .PL suffix: {suffixedTicker}");
            price = await FetchPriceAsync(suffixedTicker, symbol, cancellationToken, traceLogs);
        }

        return price;
    }

    private async Task<MarketPrice?> FetchPriceAsync(string ticker, Domain.ValueObjects.Symbol symbol, CancellationToken cancellationToken, List<string>? traceLogs)
    {
        var url = $"https://stooq.pl/q/l/?s={ticker.ToUpper()}&f=sd2t2ohlcv&h&e=csv";
        traceLogs?.Add($"Stooq: Querying URL: {url}");

        try
        {
            var csvContent = await _pipeline.ExecuteAsync(async token =>
                await _httpClient.GetStringAsync(url, token), cancellationToken);

            if (string.IsNullOrWhiteSpace(csvContent)) return null;

            var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2) return null;

            var resultLine = lines[1];
            var values = resultLine.Split(',');

            if (values.Length < 7) return null;

            if (!decimal.TryParse(values[6], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price))
            {
                var val = values[6].Trim();
                if (val == "B/D")
                {
                    traceLogs?.Add($"Stooq: No data (B/D) for {ticker}");
                }
                else
                {
                    var errorMsg = $"Could not parse price from Stooq response for {ticker}: {val}";
                    _logger.LogWarning(errorMsg);
                    traceLogs?.Add($"Stooq ERROR: {errorMsg}");
                }
                return null;
            }

            // Infer currency from ticker suffix
            var currency = "PLN"; // Default
            if (ticker.Contains('.'))
            {
                var suffix = ticker.Split('.').Last().ToUpper();
                currency = suffix switch
                {
                    "US" => "USD",
                    "UK" => "GBP",
                    "DE" => "EUR",
                    "FR" => "EUR",
                    "NL" => "EUR",
                    "JP" => "JPY",
                    "CH" => "CHF",
                    _ => "PLN"
                };
            }

            // NOTE: For Catalyst bonds, price from Stooq is a PERCENTAGE of nominal value (1000 PLN)
            // We store it as-is (e.g., 67.39) - just like purchase price
            // Conversion to absolute value happens at position VALUE calculation level, not here

            // Debug logging for Polish bonds
            var isBondOnPolishExchange = symbol.AssetType == Domain.Enums.AssetType.Bond &&
                (symbol.Exchange == "GPW" || symbol.Exchange == "WSE" || symbol.Exchange == "WAR" ||
                 symbol.Exchange == "NewConnect" || symbol.Exchange == "Catalyst");

            if (isBondOnPolishExchange || symbol.Ticker.StartsWith("FPC", StringComparison.OrdinalIgnoreCase))
            {
                traceLogs?.Add($"Stooq: Polish bond {symbol.Ticker} price = {price}% (raw percentage, no conversion here)");
                _logger.LogInformation("Stooq: Polish bond {Ticker} price = {Price}% (raw percentage)",
                    symbol.Ticker, price);
            }

            return new MarketPrice
            {
                Symbol = symbol.Ticker,
                Price = price,
                Currency = currency,
                Timestamp = DateTime.UtcNow,
                Source = "Stooq"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching price for {Symbol} from Stooq", ticker);
            traceLogs?.Add($"Stooq EXCEPTION for {ticker}: {ex.Message}");
            return null;
        }
    }

    public async Task<IEnumerable<MarketPrice>> GetHistoricalPricesAsync(Domain.ValueObjects.Symbol symbol, DateTime from, DateTime to, CancellationToken cancellationToken = default, List<string>? traceLogs = null)
    {
        // Stooq historical is harder via this API (needs different endpoint like /q/d/l/)
        // For now, we return empty as Yahoo is primary for history
        return await Task.FromResult(Enumerable.Empty<MarketPrice>());
    }

    public async Task<IEnumerable<SecurityInfo>> SearchSecuritiesAsync(string query, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(Enumerable.Empty<SecurityInfo>());
    }
}
