using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Enums;

namespace InvestmentHub.Domain.Repositories;

/// <summary>
/// Repository interface for Investment entity operations.
/// Provides data access methods for investment management.
/// </summary>
public interface IInvestmentRepository
{
    /// <summary>
    /// Gets an investment by its ID.
    /// </summary>
    /// <param name="investmentId">The investment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The investment if found, null otherwise</returns>
    Task<Investment?> GetByIdAsync(InvestmentId investmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all investments for a specific portfolio.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of investments</returns>
    Task<IEnumerable<Investment>> GetByPortfolioIdAsync(PortfolioId portfolioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all investments for a specific symbol across all portfolios.
    /// </summary>
    /// <param name="symbol">The investment symbol</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of investments</returns>
    Task<IEnumerable<Investment>> GetBySymbolAsync(Symbol symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all investments for a specific user across all portfolios.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of investments</returns>
    Task<IEnumerable<Investment>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new investment to the repository.
    /// </summary>
    /// <param name="investment">The investment to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the operation</returns>
    Task AddAsync(Investment investment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing investment in the repository.
    /// </summary>
    /// <param name="investment">The investment to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the operation</returns>
    Task UpdateAsync(Investment investment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an investment from the repository.
    /// </summary>
    /// <param name="investmentId">The investment ID to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the operation</returns>
    Task RemoveAsync(InvestmentId investmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an investment exists for a portfolio with the given symbol.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID</param>
    /// <param name="symbol">The investment symbol</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if investment exists, false otherwise</returns>
    Task<bool> ExistsBySymbolAsync(PortfolioId portfolioId, Symbol symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of investments for a specific portfolio.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The count of investments</returns>
    Task<int> GetCountByPortfolioIdAsync(PortfolioId portfolioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets investments by asset type for a specific portfolio.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID</param>
    /// <param name="assetType">The asset type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of investments</returns>
    Task<IEnumerable<Investment>> GetByAssetTypeAsync(PortfolioId portfolioId, AssetType assetType, CancellationToken cancellationToken = default);
}
