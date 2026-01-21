using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Infrastructure.Data.Repositories;

/// <summary>
/// Repository interface for Transaction aggregate operations.
/// </summary>
public interface ITransactionRepository
{
    /// <summary>
    /// Gets a transaction by its ID.
    /// </summary>
    Task<Transaction?> GetByIdAsync(TransactionId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all transactions for a portfolio.
    /// </summary>
    Task<IEnumerable<Transaction>> GetByPortfolioAsync(PortfolioId portfolioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all transactions for a specific symbol within a portfolio.
    /// </summary>
    Task<IEnumerable<Transaction>> GetBySymbolAsync(PortfolioId portfolioId, Symbol symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active (non-cancelled) transactions for a portfolio.
    /// </summary>
    Task<IEnumerable<Transaction>> GetActiveByPortfolioAsync(PortfolioId portfolioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new transaction.
    /// </summary>
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing transaction.
    /// </summary>
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
