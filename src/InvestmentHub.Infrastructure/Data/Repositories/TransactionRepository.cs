using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace InvestmentHub.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Transaction aggregate using EF Core.
/// </summary>
public class TransactionRepository : ITransactionRepository
{
    private readonly ApplicationDbContext _dbContext;

    public TransactionRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task<Transaction?> GetByIdAsync(TransactionId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Transaction>> GetByPortfolioAsync(PortfolioId portfolioId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transactions
            .Where(t => t.PortfolioId == portfolioId)
            .OrderBy(t => t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Transaction>> GetBySymbolAsync(PortfolioId portfolioId, Symbol symbol, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transactions
            .Where(t => t.PortfolioId == portfolioId && t.Symbol != null && t.Symbol.Ticker == symbol.Ticker)
            .OrderBy(t => t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Transaction>> GetActiveByPortfolioAsync(PortfolioId portfolioId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transactions
            .Where(t => t.PortfolioId == portfolioId && t.Status == TransactionStatus.Active)
            .OrderBy(t => t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _dbContext.Transactions.AddAsync(transaction, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _dbContext.Transactions.Update(transaction);
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
