using InvestmentHub.Domain.Enums;

namespace InvestmentHub.Domain.ValueObjects;

/// <summary>
/// Represents a monetary amount with currency.
/// Immutable value object that encapsulates money-related invariants and business rules.
/// </summary>
public class Money : IEquatable<Money>
{
    /// <summary>
    /// Gets the monetary amount.
    /// </summary>
    public decimal Amount { get; }
    
    /// <summary>
    /// Gets the currency of the monetary amount.
    /// </summary>
    public Currency Currency { get; }
    
        /// <summary>
        /// Initializes a new instance of the Money class.
        /// </summary>
        /// <param name="amount">The monetary amount (must be non-negative)</param>
        /// <param name="currency">The currency of the amount</param>
        /// <exception cref="ArgumentException">Thrown when amount is negative</exception>
        public Money(decimal amount, Currency currency)
        {
            if (amount < 0)
                throw new ArgumentException("Money amount cannot be negative", nameof(amount));
                
            Amount = amount;
            Currency = currency;
        }
    
    /// <summary>
    /// Creates a Money instance with zero amount.
    /// </summary>
    /// <param name="currency">The currency for the zero amount</param>
    /// <returns>A Money instance with zero amount</returns>
    public static Money Zero(Currency currency) => new(0, currency);
    
    /// <summary>
    /// Adds two Money instances of the same currency.
    /// </summary>
    /// <param name="other">The Money instance to add</param>
    /// <returns>A new Money instance with the sum</returns>
    /// <exception cref="InvalidOperationException">Thrown when currencies don't match</exception>
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add money with different currencies: {Currency} and {other.Currency}");
        
        return new Money(Amount + other.Amount, Currency);
    }
    
    /// <summary>
    /// Subtracts another Money instance from this one.
    /// </summary>
    /// <param name="other">The Money instance to subtract</param>
    /// <returns>A new Money instance with the difference</returns>
    /// <exception cref="InvalidOperationException">Thrown when currencies don't match</exception>
    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot subtract money with different currencies: {Currency} and {other.Currency}");
        
        return new Money(Amount - other.Amount, Currency);
    }
    
    /// <summary>
    /// Multiplies the Money amount by a factor.
    /// </summary>
    /// <param name="factor">The multiplication factor</param>
    /// <returns>A new Money instance with the multiplied amount</returns>
    /// <exception cref="ArgumentException">Thrown when factor is negative</exception>
    public Money Multiply(decimal factor)
    {
        if (factor < 0)
            throw new ArgumentException("Multiplication factor cannot be negative", nameof(factor));
        
        return new Money(Amount * factor, Currency);
    }
    
    /// <summary>
    /// Determines whether this Money instance equals another Money instance.
    /// </summary>
    /// <param name="other">The Money instance to compare</param>
    /// <returns>True if both amount and currency are equal</returns>
    public bool Equals(Money? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        
        return Amount == other.Amount && Currency == other.Currency;
    }
    
    /// <summary>
    /// Determines whether this Money instance equals another object.
    /// </summary>
    /// <param name="obj">The object to compare</param>
    /// <returns>True if the object is a Money instance with equal amount and currency</returns>
    public override bool Equals(object? obj) => Equals(obj as Money);
    
    /// <summary>
    /// Gets the hash code for this Money instance.
    /// </summary>
    /// <returns>A hash code based on amount and currency</returns>
    public override int GetHashCode() => HashCode.Combine(Amount, Currency);
    
    /// <summary>
    /// Returns a string representation of this Money instance.
    /// </summary>
    /// <returns>A formatted string showing amount and currency</returns>
    public override string ToString() => $"{Amount:N2} {Currency}";
    
    /// <summary>
    /// Equality operator for Money instances.
    /// </summary>
    public static bool operator ==(Money? left, Money? right) => Equals(left, right);
    
    /// <summary>
    /// Inequality operator for Money instances.
    /// </summary>
    public static bool operator !=(Money? left, Money? right) => !Equals(left, right);
}
