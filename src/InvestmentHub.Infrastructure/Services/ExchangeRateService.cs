using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Interfaces;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Marten;

namespace InvestmentHub.Infrastructure.Services;

/// <summary>
/// Service for fetching currency exchange rates and converting amounts.
/// Uses MarketPriceService to fetch currency pairs as instruments.
/// </summary>
public class ExchangeRateService : IExchangeRateService
{
    private readonly MarketPriceService _marketPriceService;
    private readonly IQuerySession _querySession;
    private readonly ILogger<ExchangeRateService> _logger;

    public ExchangeRateService(
        MarketPriceService marketPriceService,
        IQuerySession querySession,
        ILogger<ExchangeRateService> logger)
    {
        _marketPriceService = marketPriceService;
        _querySession = querySession;
        _logger = logger;
    }

    public async Task<decimal?> GetExchangeRateAsync(Currency fromCurrency, Currency toCurrency, CancellationToken cancellationToken = default)
    {
        if (fromCurrency == toCurrency) return 1.0m;

        try
        {
            // Standard pair format: EURPLN (base/quote) -> value of 1 EUR in PLN
            // Stooq: "EURPLN"
            // Yahoo: "EURPLN=X"
            var pairTickerRaw = $"{fromCurrency}{toCurrency}";

            // Try fetching from MarketPriceService (which handles provider logic)
            // We use standard ticker format first (Stooq style as it's preferred for PL defaults)
            var symbol = new Symbol(pairTickerRaw, "Currencies", AssetType.Forex);

            // Note: MarketPriceService handles provider fallback strategy.
            // Stooq expects "EURPLN", Yahoo expects "EURPLN=X".
            // However, our MarketPriceService is instrument-centric.
            // For now, we'll rely on the provider logic to interpret or we check both variations.

            // Checking standard pair
            var price = await _marketPriceService.GetCurrentPriceAsync(symbol, cancellationToken);
            if (price != null && price.Price > 0)
            {
                return price.Price;
            }

            // Fallback: Try inverse pair (e.g. PLNEUR) and invert the rate
            // This is less common for major pairs vs minor, but good for completeness
            var inverseTickerRaw = $"{toCurrency}{fromCurrency}";
            var inverseSymbol = new Symbol(inverseTickerRaw, "Currencies", AssetType.Forex);

            var inversePrice = await _marketPriceService.GetCurrentPriceAsync(inverseSymbol, cancellationToken);
            if (inversePrice != null && inversePrice.Price > 0)
            {
                return 1.0m / inversePrice.Price;
            }

            _logger.LogWarning("Could not find exchange rate for {From}->{To}", fromCurrency, toCurrency);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching exchange rate for {From}->{To}", fromCurrency, toCurrency);
            return null;
        }
    }

    public async Task<Money?> ConvertAsync(decimal amount, Currency fromCurrency, Currency toCurrency, CancellationToken cancellationToken = default)
    {
        if (fromCurrency == toCurrency)
        {
            return new Money(amount, toCurrency);
        }

        var rate = await GetExchangeRateAsync(fromCurrency, toCurrency, cancellationToken);
        if (rate.HasValue)
        {
            var convertedAmount = amount * rate.Value;
            return new Money(convertedAmount, toCurrency);
        }

        return null;
    }

    public async Task<decimal?> GetHistoricalExchangeRateAsync(Currency fromCurrency, Currency toCurrency, DateTime date, CancellationToken cancellationToken = default)
    {
        if (fromCurrency == toCurrency) return 1.0m;

        try
        {
            // Normalize date to UTC/Midnight
            var queryDate = date.Date;

            // Strategy: Check local DB (Marten) for PriceHistory
            // We look for dates <= queryDate to find the closest previous rate (known as "As Of" date)
            // Limit lookback to e.g. 7 days to avoid using very stale data
            var lookbackDate = queryDate.AddDays(-7);

            var pairTickerRaw = $"{fromCurrency}{toCurrency}"; // e.g. EURPLN

            var rate = await GetRateFromDbAsync(pairTickerRaw, queryDate, lookbackDate, cancellationToken);
            if (rate.HasValue) return rate;

            // Try inverse
            var inverseTickerRaw = $"{toCurrency}{fromCurrency}"; // e.g. PLNEUR
            var inverseRate = await GetRateFromDbAsync(inverseTickerRaw, queryDate, lookbackDate, cancellationToken);
            if (inverseRate.HasValue && inverseRate.Value > 0) return 1.0m / inverseRate.Value;

            _logger.LogWarning("No historical rate found for {From}->{To} on {Date}", fromCurrency, toCurrency, date.ToShortDateString());
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching historical exchange rate for {From}->{To} on {Date}", fromCurrency, toCurrency, date);
            return null;
        }
    }

    private async Task<decimal?> GetRateFromDbAsync(string ticker, DateTime maxDate, DateTime minDate, CancellationToken token)
    {
        var history = await _querySession.Query<PriceHistory>()
            .Where(x => x.Symbol == ticker && x.Date <= maxDate && x.Date >= minDate)
            .OrderByDescending(x => x.Date)
            .FirstOrDefaultAsync(token);

        return history?.Close;
    }
}
