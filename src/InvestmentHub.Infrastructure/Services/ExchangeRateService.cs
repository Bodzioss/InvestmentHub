using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Interfaces;
using InvestmentHub.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.Services;

/// <summary>
/// Service for fetching currency exchange rates and converting amounts.
/// Uses MarketPriceService to fetch currency pairs as instruments.
/// </summary>
public class ExchangeRateService : IExchangeRateService
{
    private readonly MarketPriceService _marketPriceService;
    private readonly ILogger<ExchangeRateService> _logger;

    public ExchangeRateService(
        MarketPriceService marketPriceService,
        ILogger<ExchangeRateService> logger)
    {
        _marketPriceService = marketPriceService;
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
}
