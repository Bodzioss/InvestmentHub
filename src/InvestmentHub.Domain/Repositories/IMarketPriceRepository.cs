using InvestmentHub.Domain.Entities;

namespace InvestmentHub.Domain.Repositories;

/// <summary>
/// Repository interface for cached market prices.
/// </summary>
public interface IMarketPriceRepository
{
    /// <summary>
    /// Gets the most recent cached price for a symbol.
    /// </summary>
    Task<CachedMarketPrice?> GetLatestPriceAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets today's cached price for a symbol (if it exists).
    /// </summary>
    Task<CachedMarketPrice?> GetTodaysPriceAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cached prices for today for the specified symbols.
    /// </summary>
    Task<List<CachedMarketPrice>> GetTodaysPricesAsync(List<string> symbols, CancellationToken cancellationToken);

    /// <summary>
    /// Gets price history for a symbol within a date range.
    /// </summary>
    /// <param name="symbol">The ticker symbol</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of cached prices ordered by date</returns>
    Task<List<CachedMarketPrice>> GetPriceHistoryAsync(string symbol, DateTime startDate, DateTime endDate, CancellationToken cancellationToken);

    /// <summary>
    /// Saves a new price to the cache.
    /// </summary>
    Task SavePriceAsync(CachedMarketPrice price, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes cached prices older than the specified date.
    /// </summary>
    Task DeleteOldPricesAsync(DateTime cutoffDate, CancellationToken cancellationToken);
}
