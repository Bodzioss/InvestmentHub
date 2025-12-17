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
        return await _context.CachedMarketPrices
            .Where(p => p.Symbol == symbol && p.FetchedAt >= startDate && p.FetchedAt <= endDate)
            .OrderBy(p => p.FetchedAt)
            .ToListAsync(cancellationToken);
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
