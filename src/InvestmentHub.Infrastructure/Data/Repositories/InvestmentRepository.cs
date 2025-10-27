using Microsoft.EntityFrameworkCore;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Repositories;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Enums;

namespace InvestmentHub.Infrastructure.Data.Repositories;

/// <summary>
/// Entity Framework Core implementation of the IInvestmentRepository.
/// Provides data access methods for Investment entity operations.
/// </summary>
public class InvestmentRepository : IInvestmentRepository
{
    private readonly ApplicationDbContext _context;

    public InvestmentRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<Investment?> GetByIdAsync(InvestmentId investmentId, CancellationToken cancellationToken = default)
    {
        return await _context.Investments
            .FirstOrDefaultAsync(i => i.Id == investmentId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Investment>> GetByPortfolioIdAsync(PortfolioId portfolioId, CancellationToken cancellationToken = default)
    {
        return await _context.Investments
            .Where(i => i.PortfolioId == portfolioId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Investment>> GetBySymbolAsync(Symbol symbol, CancellationToken cancellationToken = default)
    {
        return await _context.Investments
            .Where(i => i.Symbol.Ticker == symbol.Ticker && 
                       i.Symbol.Exchange == symbol.Exchange &&
                       i.Symbol.AssetType == symbol.AssetType)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Investment>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        // Get all portfolios for the user first
        var portfolioIds = await _context.Portfolios
            .Where(p => p.OwnerId == userId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        // Get all investments for those portfolios
        return await _context.Investments
            .Where(i => portfolioIds.Contains(i.PortfolioId))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(Investment investment, CancellationToken cancellationToken = default)
    {
        await _context.Investments.AddAsync(investment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Investment investment, CancellationToken cancellationToken = default)
    {
        _context.Investments.Update(investment);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(InvestmentId investmentId, CancellationToken cancellationToken = default)
    {
        var investment = await GetByIdAsync(investmentId, cancellationToken);
        if (investment != null)
        {
            _context.Investments.Remove(investment);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsBySymbolAsync(PortfolioId portfolioId, Symbol symbol, CancellationToken cancellationToken = default)
    {
        return await _context.Investments
            .AnyAsync(i => i.PortfolioId == portfolioId &&
                          i.Symbol.Ticker == symbol.Ticker &&
                          i.Symbol.Exchange == symbol.Exchange &&
                          i.Symbol.AssetType == symbol.AssetType,
                     cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetCountByPortfolioIdAsync(PortfolioId portfolioId, CancellationToken cancellationToken = default)
    {
        return await _context.Investments
            .CountAsync(i => i.PortfolioId == portfolioId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Investment>> GetByAssetTypeAsync(PortfolioId portfolioId, AssetType assetType, CancellationToken cancellationToken = default)
    {
        return await _context.Investments
            .Where(i => i.PortfolioId == portfolioId && i.Symbol.AssetType == assetType)
            .ToListAsync(cancellationToken);
    }
}

