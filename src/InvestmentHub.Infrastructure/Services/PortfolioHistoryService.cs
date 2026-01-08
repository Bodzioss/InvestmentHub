using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Interfaces;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Infrastructure.Data;
using Marten;
using Marten.Pagination; // Ensure Marten extensions are available if needed, though 'using Marten' usually suffices
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.Services;

public class PortfolioHistoryService : IPortfolioHistoryService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IQuerySession _querySession; // Marten
    private readonly ILogger<PortfolioHistoryService> _logger;

    public PortfolioHistoryService(
        ApplicationDbContext dbContext,
        IQuerySession querySession,
        ILogger<PortfolioHistoryService> logger)
    {
        _dbContext = dbContext;
        _querySession = querySession;
        _logger = logger;
    }

    public async Task<IEnumerable<DatePoint>> GetPortfolioHistoryAsync(Guid portfolioId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating portfolio history for {PortfolioId} from {From} to {To}", portfolioId, from, to);

        // 1. Fetch all transactions
        var transactions = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            _dbContext.Transactions
            .Where(t => t.PortfolioId == new PortfolioId(portfolioId) && t.Status == TransactionStatus.Active)
            .OrderBy(t => t.TransactionDate),
            cancellationToken);

        if (!transactions.Any())
        {
            _logger.LogWarning("No transactions found for portfolio {PortfolioId}", portfolioId);
            return Enumerable.Empty<DatePoint>();
        }

        // Adjust 'from' date if it's before the first transaction (optional, but cleaner)
        var firstTxDate = transactions[0].TransactionDate.Date;
        if (from < firstTxDate) from = firstTxDate;
        if (from > to) return Enumerable.Empty<DatePoint>();

        // 2. Identify unique assets and currencies
        var assets = transactions
            .Where(t => t.Type == TransactionType.BUY || t.Type == TransactionType.SELL)
            .Select(t => t.Symbol)
            .DistinctBy(s => s.Ticker)
            .ToList();

        // 3. Pre-fetch historical data for all involved assets
        var priceCache = new Dictionary<string, Dictionary<DateTime, decimal>>();

        // Assets
        foreach (var asset in assets)
        {
            var prices = await FetchPriceHistoryAsync(asset.Ticker, from, to, cancellationToken);
            priceCache[asset.Ticker] = prices;
            if (!prices.Any())
            {
                _logger.LogWarning("No historical prices found for {Ticker} from {From} to {To}", asset.Ticker, from, to);
            }
        }

        // Currencies (EURPLN etc.)
        var currencyCache = new Dictionary<string, Dictionary<DateTime, decimal>>();
        var usedCurrencies = transactions
            .Where(t => t.PricePerUnit != null)
            .Select(t => t.PricePerUnit!.Currency)
            .Distinct()
            .Where(c => c != Currency.PLN);

        foreach (var currency in usedCurrencies)
        {
            var pairTicker = $"{currency}PLN"; // Standard
            var rates = await FetchPriceHistoryAsync(pairTicker, from, to, cancellationToken);

            // If failed, try inverse PLNEUR and invert
            if (!rates.Any())
            {
                var inverseTicker = $"PLN{currency}";
                var inverseRates = await FetchPriceHistoryAsync(inverseTicker, from, to, cancellationToken);
                if (inverseRates.Any())
                {
                    rates = inverseRates.ToDictionary(k => k.Key, v => v.Value > 0 ? 1 / v.Value : 0);
                }
            }

            currencyCache[currency.ToString()] = rates;
        }

        // 4. Replay Loop
        var result = new List<DatePoint>();

        // Track holdings: Ticker -> Quantity
        var currentHoldings = new Dictionary<string, decimal>();

        // Optimize transaction processing pointer
        int txIndex = 0;

        for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
        {
            // Apply transactions for this day
            while (txIndex < transactions.Count && transactions[txIndex].TransactionDate.Date <= date)
            {
                var tx = transactions[txIndex];
                ProcessTransaction(tx, currentHoldings);
                txIndex++;
            }

            // Calculate Portfolio Value for this day
            decimal totalValuePLN = 0;

            foreach (var holding in currentHoldings)
            {
                var ticker = holding.Key;
                var qty = holding.Value;

                if (qty == 0) continue;

                // Get Price
                decimal price = 0;
                if (priceCache.TryGetValue(ticker, out var dates) && dates.TryGetValue(date, out var dayPrice))
                {
                    price = dayPrice;
                }
                else
                {
                    // Fallback to previous known price (Forward fill)
                    price = GetLastKnownPrice(priceCache, ticker, date);
                }

                // Determine Currency of the asset
                // Logic: Find ANY transaction for this ticker to guess the currency.
                // Assuming an asset doesn't change trading currency.
                var assetTx = transactions.FirstOrDefault(t => t.Symbol.Ticker == ticker && t.PricePerUnit != null);
                var assetCurrency = assetTx?.PricePerUnit?.Currency ?? Currency.PLN;

                // Value in original currency
                var valueNative = qty * price;

                // Convert to PLN
                if (assetCurrency == Currency.PLN)
                {
                    totalValuePLN += valueNative;
                }
                else
                {
                    decimal rate = 1.0m;
                    if (currencyCache.TryGetValue(assetCurrency.ToString(), out var rates) && rates.TryGetValue(date, out var dayRate))
                    {
                        rate = dayRate;
                    }
                    else
                    {
                        rate = GetLastKnownPrice(currencyCache, assetCurrency.ToString(), date);
                        if (rate == 0) rate = 1.0m;
                    }

                    totalValuePLN += valueNative * rate;
                }
            }

            if (totalValuePLN == 0 && currentHoldings.Values.Any(v => v > 0))
            {
                // Sparse logging for zero value despite holdings
                if (date.Day == 1)
                {
                    _logger.LogWarning("Zero value calculated for {Date} despite active holdings", date);
                    foreach (var holding in currentHoldings.Where(h => h.Value > 0))
                    {
                        decimal p = 0;
                        if (priceCache.TryGetValue(holding.Key, out var d) && d.TryGetValue(date, out var val)) p = val;
                        else p = GetLastKnownPrice(priceCache, holding.Key, date);

                        _logger.LogWarning("   Asset: {Ticker}, Qty: {Qty}, Price: {Price}", holding.Key, holding.Value, p);
                    }
                }
            }
            result.Add(new DatePoint(date, totalValuePLN));
        }

        return result;
    }

    private void ProcessTransaction(InvestmentHub.Domain.Aggregates.Transaction tx, Dictionary<string, decimal> holdings)
    {
        if (!holdings.ContainsKey(tx.Symbol.Ticker)) holdings[tx.Symbol.Ticker] = 0;

        if (tx.Type == TransactionType.BUY)
        {
            holdings[tx.Symbol.Ticker] += tx.Quantity ?? 0;
        }
        else if (tx.Type == TransactionType.SELL)
        {
            holdings[tx.Symbol.Ticker] -= tx.Quantity ?? 0;
        }
    }

    private decimal GetLastKnownPrice(Dictionary<string, Dictionary<DateTime, decimal>> cache, string key, DateTime date)
    {
        if (!cache.TryGetValue(key, out var dates)) return 0;

        for (int i = 0; i < 7; i++)
        {
            var d = date.AddDays(-i);
            if (dates.TryGetValue(d, out var val)) return val;
        }
        return 0;
    }

    private async Task<Dictionary<DateTime, decimal>> FetchPriceHistoryAsync(string ticker, DateTime from, DateTime to, CancellationToken token)
    {
        // Add buffer for lookback
        var searchFrom = from.AddDays(-10);

        var history = await _querySession.Query<PriceHistory>()
            .Where(x => x.Symbol == ticker && x.Date >= searchFrom && x.Date <= to)
            .OrderBy(x => x.Date)
            .ToListAsync(token);

        // Deduplicate by Date (take last if duplicates)
        return history
            .GroupBy(x => x.Date.Date)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Date).LastOrDefault()?.Close ?? 0);
    }
}
