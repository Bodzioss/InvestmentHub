using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Interfaces;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Infrastructure.Data;
using Marten;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.Jobs;

public class HistoricalImportJob
{
    private readonly IDocumentStore _documentStore;
    private readonly ApplicationDbContext _dbContext;
    private readonly IEnumerable<IMarketDataProvider> _marketDataProviders;
    private readonly ILogger<HistoricalImportJob> _logger;

    public HistoricalImportJob(
        IDocumentStore documentStore,
        ApplicationDbContext dbContext,
        IEnumerable<IMarketDataProvider> marketDataProviders,
        ILogger<HistoricalImportJob> logger)
    {
        _documentStore = documentStore;
        _dbContext = dbContext;
        _marketDataProviders = marketDataProviders;
        _logger = logger;
    }

    public async Task ImportPortfolioHistoryAsync(Guid portfolioId)
    {
        _logger.LogInformation("Starting portfolio history import for {PortfolioId}", portfolioId);

        // 1. Import Assets (Union of Transactions and Investments to cover all bases)
        var transactionTickers = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            _dbContext.Transactions
            .Where(t => t.PortfolioId == new PortfolioId(portfolioId))
            .Select(t => t.Symbol.Ticker));

        var investmentTickers = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            _dbContext.Investments
            .Where(i => i.PortfolioId == new PortfolioId(portfolioId))
            .Select(i => i.Symbol.Ticker));

        var uniqueTickers = transactionTickers.Concat(investmentTickers).Distinct().ToList();

        _logger.LogInformation("Found {Count} unique assets for history import", uniqueTickers.Count);

        foreach (var ticker in uniqueTickers)
        {
            await ImportHistoryAsync(ticker);
        }

        // 2. Import Currencies
        var transactionCurrencies = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            _dbContext.Transactions
            .Where(t => t.PortfolioId == new PortfolioId(portfolioId) && t.PricePerUnit != null)
            .Select(t => t.PricePerUnit!.Currency));

        var investmentCurrencies = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            _dbContext.Investments
            .Where(i => i.PortfolioId == new PortfolioId(portfolioId))
            .Select(i => i.CurrentValue.Currency));

        var uniqueCurrencies = transactionCurrencies.Concat(investmentCurrencies).Distinct().ToList();

        foreach (var currency in uniqueCurrencies)
        {
            if (currency != Currency.PLN)
            {
                await ImportCurrencyHistoryAsync(currency, Currency.PLN);
            }
        }

        _logger.LogInformation("Portfolio history import completed for {PortfolioId}", portfolioId);
    }

    public async Task ImportCurrencyHistoryAsync(Currency from, Currency to)
    {
        var ticker = $"{from}{to}"; // Canonical ticker: EURPLN
        var symbol = new Symbol(ticker, "Currencies", AssetType.Forex);

        await ImportHistoryInternalAsync(symbol, ticker);
    }

    public async Task ImportHistoryAsync(string ticker)
    {
        // Try to find instrument to get symbol context
        var instrument = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_dbContext.Instruments, i => i.Symbol.Ticker == ticker);
        var symbol = instrument?.Symbol ?? new Symbol(ticker, "GPW", AssetType.Stock);

        await ImportHistoryInternalAsync(symbol, ticker);
    }

    private async Task ImportHistoryInternalAsync(Symbol symbol, string canonicalTicker)
    {
        _logger.LogInformation("Starting HistoricalImportJob for {Symbol}...", canonicalTicker);

        // 1. Fetch 5 years of history
        var to = DateTime.UtcNow;
        var from = to.AddYears(-5);
        List<InvestmentHub.Domain.Entities.MarketPrice> historyList = new();

        foreach (var provider in _marketDataProviders)
        {
            try
            {
                var history = await provider.GetHistoricalPricesAsync(symbol, from, to, CancellationToken.None);
                historyList = history.ToList();
                if (historyList.Any())
                {
                    _logger.LogInformation("Fetched {Count} historical records from {Provider}", historyList.Count, provider.GetType().Name);
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider {Provider} failed for {Symbol}", provider.GetType().Name, canonicalTicker);
            }
        }

        if (!historyList.Any())
        {
            // For Currencies, try Yahoo fallback format if needed
            if (symbol.AssetType == AssetType.Forex && !canonicalTicker.Contains("=X"))
            {
                var yahooSymbol = new Symbol($"{canonicalTicker}=X", "Yahoo", AssetType.Forex);
                foreach (var provider in _marketDataProviders)
                {
                    var history = await provider.GetHistoricalPricesAsync(yahooSymbol, from, to, CancellationToken.None);
                    historyList = history.ToList();
                    if (historyList.Any()) break;
                }
            }

            if (!historyList.Any())
            {
                _logger.LogWarning("No historical data found for {Symbol} from any provider", canonicalTicker);
                return;
            }
        }

        // 2. Store in Marten
        using var session = _documentStore.LightweightSession();

        session.DeleteWhere<PriceHistory>(x => x.Symbol == canonicalTicker);

        var priceHistoryRecords = historyList.Select(h => new PriceHistory
        {
            Id = Guid.NewGuid(),
            Symbol = canonicalTicker, // Use canonical ticker to ensure consistent lookups
            Date = h.Timestamp.Date,
            Open = h.Open ?? 0,
            High = h.High ?? 0,
            Low = h.Low ?? 0,
            Close = h.Close ?? 0,
            Volume = h.Volume ?? 0,
            Source = h.Source,
            LastUpdated = DateTime.UtcNow
        });

        session.Store(priceHistoryRecords);
        await session.SaveChangesAsync();

        _logger.LogInformation("Successfully stored historical data for {Symbol}", canonicalTicker);
    }
}
