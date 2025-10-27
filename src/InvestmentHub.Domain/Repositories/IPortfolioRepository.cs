using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Repositories;

/// <summary>
/// Repository interface for Portfolio aggregate operations.
/// Provides data access methods for portfolio management.
/// </summary>
public interface IPortfolioRepository
{
    /// <summary>
    /// Gets a portfolio by its ID.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The portfolio if found, null otherwise</returns>
    Task<Portfolio?> GetByIdAsync(PortfolioId portfolioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all portfolios for a specific user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of portfolios</returns>
    Task<IEnumerable<Portfolio>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new portfolio to the repository.
    /// </summary>
    /// <param name="portfolio">The portfolio to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the operation</returns>
    Task AddAsync(Portfolio portfolio, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing portfolio in the repository.
    /// </summary>
    /// <param name="portfolio">The portfolio to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the operation</returns>
    Task UpdateAsync(Portfolio portfolio, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a portfolio from the repository.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the operation</returns>
    Task RemoveAsync(PortfolioId portfolioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a portfolio exists for a user with the given name.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="name">The portfolio name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if portfolio exists, false otherwise</returns>
    Task<bool> ExistsByNameAsync(UserId userId, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of portfolios for a specific user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The count of portfolios</returns>
    Task<int> GetCountByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
}
