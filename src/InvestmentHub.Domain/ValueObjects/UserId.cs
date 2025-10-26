namespace InvestmentHub.Domain.ValueObjects;

/// <summary>
/// Represents a unique identifier for a user.
/// Immutable value object that wraps a GUID to provide type safety.
/// </summary>
/// <param name="Value">The GUID value representing the user ID</param>
public record UserId(Guid Value)
{
    /// <summary>
    /// Creates a new UserId with a random GUID.
    /// </summary>
    /// <returns>A new UserId instance</returns>
    public static UserId New() => new(Guid.NewGuid());
    
    /// <summary>
    /// Creates a UserId from a string representation of a GUID.
    /// </summary>
    /// <param name="value">String representation of the GUID</param>
    /// <returns>UserId instance</returns>
    /// <exception cref="ArgumentException">Thrown when the string is not a valid GUID</exception>
    public static UserId FromString(string value)
    {
        if (!Guid.TryParse(value, out var guid))
            throw new ArgumentException("Invalid GUID format", nameof(value));
        
        return new UserId(guid);
    }
}
