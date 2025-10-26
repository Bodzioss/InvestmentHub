namespace InvestmentHub.Domain.ValueObjects;

/// <summary>
/// Represents a unique identifier for a portfolio.
/// Immutable value object that wraps a GUID to provide type safety.
/// </summary>
/// <param name="Value">The GUID value representing the portfolio ID</param>
public record PortfolioId(Guid Value)
{
    /// <summary>
    /// Creates a new PortfolioId with a random GUID.
    /// </summary>
    /// <returns>A new PortfolioId instance</returns>
    public static PortfolioId New() => new(Guid.NewGuid());
    
    /// <summary>
    /// Creates a PortfolioId from a string representation of a GUID.
    /// </summary>
    /// <param name="value">String representation of the GUID</param>
    /// <returns>PortfolioId instance</returns>
    /// <exception cref="ArgumentException">Thrown when the string is not a valid GUID</exception>
    public static PortfolioId FromString(string value)
    {
        if (!Guid.TryParse(value, out var guid))
            throw new ArgumentException("Invalid GUID format", nameof(value));
        
        return new PortfolioId(guid);
    }
}
