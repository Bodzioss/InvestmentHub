using Microsoft.EntityFrameworkCore;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Repositories;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Infrastructure.Data.Repositories;

/// <summary>
/// Entity Framework Core implementation of the IPortfolioRepository.
/// Provides data access methods for Portfolio aggregate operations.
/// </summary>
public class PortfolioRepository : IPortfolioRepository
{
    private readonly ApplicationDbContext _context;

    public PortfolioRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<Portfolio?> GetByIdAsync(PortfolioId portfolioId, CancellationToken cancellationToken = default)
    {
        // Note: EF Core doesn't load the Investments collection as it's ignored in the entity configuration
        // The Portfolio's internal _investments list won't be populated from the database
        // This is a limitation of using private collections with EF Core
        return await _context.Portfolios
            .FirstOrDefaultAsync(p => p.Id == portfolioId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Portfolio>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await _context.Portfolios
            .Where(p => p.OwnerId == userId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(Portfolio portfolio, CancellationToken cancellationToken = default)
    {
        await _context.Portfolios.AddAsync(portfolio, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Portfolio portfolio, CancellationToken cancellationToken = default)
    {
        _context.Portfolios.Update(portfolio);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(PortfolioId portfolioId, CancellationToken cancellationToken = default)
    {
        var portfolio = await GetByIdAsync(portfolioId, cancellationToken);
        if (portfolio != null)
        {
            _context.Portfolios.Remove(portfolio);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByNameAsync(UserId userId, string name, CancellationToken cancellationToken = default)
    {
        return await _context.Portfolios
            .AnyAsync(p => p.OwnerId == userId && p.Name == name, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetCountByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await _context.Portfolios
            .CountAsync(p => p.OwnerId == userId, cancellationToken);
    }
}

