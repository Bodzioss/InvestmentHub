using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Interfaces;
using Marten;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.Jobs;

public class HistoricalImportJob
{
    private readonly IDocumentStore _documentStore;
    private readonly IMarketDataProvider _marketDataProvider;
    private readonly ILogger<HistoricalImportJob> _logger;

    public HistoricalImportJob(
        IDocumentStore documentStore,
        IMarketDataProvider marketDataProvider,
        ILogger<HistoricalImportJob> _logger)
    {
        _documentStore = documentStore;
        _marketDataProvider = marketDataProvider;
        this._logger = _logger;
    }

    public async Task ImportHistoryAsync(string symbol)
    {
        _logger.LogInformation("Starting HistoricalImportJob for {Symbol}...", symbol);

        try
        {
            // 1. Fetch 5 years of history
            var to = DateTime.UtcNow;
            var from = to.AddYears(-5);

            var history = await _marketDataProvider.GetHistoricalPricesAsync(symbol, from, to, CancellationToken.None);
            var historyList = history.ToList();

            if (!historyList.Any())
            {
                _logger.LogWarning("No historical data found for {Symbol}", symbol);
                return;
            }

            _logger.LogInformation("Fetched {Count} historical records for {Symbol}", historyList.Count, symbol);

            // 2. Store in Marten
            // We use LightweightSession for better performance on bulk inserts
            using var session = _documentStore.LightweightSession();

            // Check if we already have data to avoid duplicates or decide on update strategy
            // For simplicity, we'll upsert based on Symbol + Date logic if we had a composite key,
            // but Marten uses Guid Id.
            // Strategy: Delete existing for this symbol and re-insert (simplest for full refresh)
            // OR: Check existence.
            // Let's go with: Delete existing history for this symbol and insert new.
            // This is safe for a "Import" job.
            
            session.DeleteWhere<PriceHistory>(x => x.Symbol == symbol);

            var priceHistoryRecords = historyList.Select(h => new PriceHistory
            {
                Id = Guid.NewGuid(),
                Symbol = h.Symbol,
                Date = h.Timestamp.Date, // Ensure we store just date part if needed, or full timestamp
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

            _logger.LogInformation("Successfully stored historical data for {Symbol}", symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HistoricalImportJob failed for {Symbol}", symbol);
            throw;
        }
    }
}
