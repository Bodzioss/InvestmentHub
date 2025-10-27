using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Entities;

namespace InvestmentHub.Domain.Repositories;

/// <summary>
/// Repository interface for User entity operations.
/// Provides data access methods for user management.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets all users.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of all users</returns>
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by its ID.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email address.
    /// </summary>
    /// <param name="email">The user's email address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user exists.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user exists, false otherwise</returns>
    Task<bool> ExistsAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of portfolios for a specific user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The count of portfolios</returns>
    Task<int> GetPortfolioCountAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user can create more portfolios (based on limits).
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="maxPortfolios">Maximum allowed portfolios</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user can create more portfolios, false otherwise</returns>
    Task<bool> CanCreatePortfolioAsync(UserId userId, int maxPortfolios = 10, CancellationToken cancellationToken = default);
}
