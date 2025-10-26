namespace InvestmentHub.Domain.ValueObjects;

/// <summary>
/// Represents a unique identifier for an investment.
/// Immutable value object that wraps a GUID to provide type safety.
/// </summary>
/// <param name="Value">The GUID value representing the investment ID</param>
public record InvestmentId(Guid Value)
{
    /// <summary>
    /// Creates a new InvestmentId with a random GUID.
    /// </summary>
    /// <returns>A new InvestmentId instance</returns>
    public static InvestmentId New() => new(Guid.NewGuid());
    
    /// <summary>
    /// Creates an InvestmentId from a string representation of a GUID.
    /// </summary>
    /// <param name="value">String representation of the GUID</param>
    /// <returns>InvestmentId instance</returns>
    /// <exception cref="ArgumentException">Thrown when the string is not a valid GUID</exception>
    public static InvestmentId FromString(string value)
    {
        if (!Guid.TryParse(value, out var guid))
            throw new ArgumentException("Invalid GUID format", nameof(value));
        
        return new InvestmentId(guid);
    }
}
