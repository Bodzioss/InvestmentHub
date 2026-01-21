using System.Diagnostics.CodeAnalysis;

namespace InvestmentHub.Domain.ValueObjects;

/// <summary>
/// Value object representing a unique transaction identifier
/// </summary>
public record TransactionId(Guid Value)
{
    /// <summary>
    /// Creates a new TransactionId with a new GUID
    /// </summary>
    public static TransactionId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a TransactionId from a string representation
    /// </summary>
    public static TransactionId FromString(string id)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            throw new ArgumentException($"Invalid TransactionId format: {id}", nameof(id));
        }
        return new TransactionId(guid);
    }

    /// <summary>
    /// Tries to create a TransactionId from a string
    /// </summary>
    public static bool TryParse(string? value, [NotNullWhen(true)] out TransactionId? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value) || !Guid.TryParse(value, out var guid))
        {
            return false;
        }
        result = new TransactionId(guid);
        return true;
    }
}
