using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Repositories;
using InvestmentHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvestmentHub.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for cached market prices.
/// </summary>
public class MarketPriceRepository : IMarketPriceRepository
{
    private readonly ApplicationDbContext _context;

    public MarketPriceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CachedMarketPrice?> GetLatestPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        return await _context.CachedMarketPrices
            .Where(p => p.Symbol == symbol)
            .OrderByDescending(p => p.FetchedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CachedMarketPrice?> GetTodaysPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        return await _context.CachedMarketPrices
            .Where(p => p.Symbol == symbol && p.FetchedAt >= today && p.FetchedAt < tomorrow)
            .OrderByDescending(p => p.FetchedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<CachedMarketPrice>> GetTodaysPricesAsync(List<string> symbols, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        return await _context.CachedMarketPrices
            .Where(p => symbols.Contains(p.Symbol) && p.FetchedAt >= today && p.FetchedAt < tomorrow)
            .OrderByDescending(p => p.FetchedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CachedMarketPrice>> GetPriceHistoryAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        // Normalize dates to start of day for proper comparison
        // Important: PostgreSQL requires DateTimes with Kind=UTC, so we use SpecifyKind
        var normalizedStart = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc);
        var normalizedEnd = DateTime.SpecifyKind(endDate.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

        // Group by date and take the most recent price for each day
        // This ensures we get only one price per day, avoiding duplicate values for today/yesterday
        var prices = await _context.CachedMarketPrices
            .Where(p => p.Symbol == symbol && p.FetchedAt >= normalizedStart && p.FetchedAt <= normalizedEnd)
            .ToListAsync(cancellationToken);

        // Group by date (not datetime) and select the latest price for each day
        return prices
            .GroupBy(p => p.FetchedAt.Date)
            .Select(g => g.OrderByDescending(p => p.FetchedAt).First())
            .OrderBy(p => p.FetchedAt)
            .ToList();
    }

    public async Task SavePriceAsync(CachedMarketPrice price, CancellationToken cancellationToken = default)
    {
        _context.CachedMarketPrices.Add(price);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteOldPricesAsync(DateTime cutoffDate, CancellationToken cancellationToken)
    {
        var oldPrices = await _context.CachedMarketPrices
            .Where(p => p.FetchedAt < cutoffDate)
            .ToListAsync(cancellationToken);

        _context.CachedMarketPrices.RemoveRange(oldPrices);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
