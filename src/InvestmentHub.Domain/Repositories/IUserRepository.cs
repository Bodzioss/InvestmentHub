using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Repositories;

/// <summary>
/// Repository interface for User entity operations.
/// Provides data access methods for user management.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by its ID.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default);

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

/// <summary>
/// Simple User entity for repository operations.
/// In a real application, this would be a full domain entity.
/// </summary>
public class User
{
    /// <summary>
    /// Gets the user ID.
    /// </summary>
    public UserId Id { get; }

    /// <summary>
    /// Gets the user name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the user email.
    /// </summary>
    public string Email { get; }

    /// <summary>
    /// Gets the user creation date.
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Initializes a new instance of the User class.
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <param name="name">The user name</param>
    /// <param name="email">The user email</param>
    /// <param name="createdAt">The creation date</param>
    public User(UserId id, string name, string email, DateTime createdAt)
    {
        Id = id;
        Name = name;
        Email = email;
        CreatedAt = createdAt;
    }
}
