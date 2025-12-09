using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Entities;

/// <summary>
/// Represents a user in the InvestmentHub system.
/// </summary>
public class User
{
    /// <summary>
    /// Gets the user ID.
    /// </summary>
    public UserId Id { get; private set; }

    /// <summary>
    /// Gets the user name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the user email.
    /// </summary>
    public string Email { get; private set; }

    /// <summary>
    /// Gets the user creation date.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Private parameterless constructor for EF Core.
    /// </summary>
    private User()
    {
        // EF Core requires a parameterless constructor
        // Values will be set through properties
        Id = null!;
        Name = null!;
        Email = null!;
    }

    /// <summary>
    /// Initializes a new instance of the User class.
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <param name="name">The user name</param>
    /// <param name="email">The user email</param>
    /// <param name="createdAt">The creation date</param>
    public User(UserId id, string name, string email, DateTime createdAt)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        CreatedAt = createdAt;
    }
}
