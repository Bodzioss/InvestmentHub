using InvestmentHub.Domain.Enums;

namespace InvestmentHub.Domain.ValueObjects;

/// <summary>
/// Represents a financial instrument symbol with exchange and asset type information.
/// Immutable value object that encapsulates symbol-related invariants and business rules.
/// </summary>
public sealed class Symbol : IEquatable<Symbol>
{
    /// <summary>
    /// Gets the ticker symbol (e.g., "AAPL", "MSFT").
    /// </summary>
    public string Ticker { get; }

    /// <summary>
    /// Gets the exchange where the symbol is traded (e.g., "NASDAQ", "NYSE").
    /// </summary>
    public string Exchange { get; }

    /// <summary>
    /// Gets the type of asset this symbol represents.
    /// </summary>
    public AssetType AssetType { get; }

    /// <summary>
    /// Initializes a new instance of the Symbol class.
    /// </summary>
    /// <param name="ticker">The ticker symbol (1-10 characters, will be converted to uppercase)</param>
    /// <param name="exchange">The exchange code (cannot be null or empty)</param>
    /// <param name="assetType">The type of asset</param>
    /// <exception cref="ArgumentException">Thrown when ticker or exchange is invalid</exception>
    public Symbol(string ticker, string exchange, AssetType assetType)
    {
        if (string.IsNullOrWhiteSpace(ticker))
            throw new ArgumentException("Ticker cannot be null or empty", nameof(ticker));

        if (ticker.Length > 50)
            throw new ArgumentException("Ticker cannot exceed 50 characters", nameof(ticker));

        if (string.IsNullOrWhiteSpace(exchange))
            throw new ArgumentException("Exchange cannot be null or empty", nameof(exchange));

        Ticker = ticker.ToUpperInvariant();
        Exchange = exchange.ToUpperInvariant();
        AssetType = assetType;
    }

    /// <summary>
    /// Creates a Symbol for a stock.
    /// </summary>
    /// <param name="ticker">The stock ticker symbol</param>
    /// <param name="exchange">The exchange code</param>
    /// <returns>A Symbol instance for a stock</returns>
    public static Symbol Stock(string ticker, string exchange) => new(ticker, exchange, AssetType.Stock);

    /// <summary>
    /// Creates a Symbol for a cryptocurrency.
    /// </summary>
    /// <param name="ticker">The crypto ticker symbol</param>
    /// <param name="exchange">The exchange code</param>
    /// <returns>A Symbol instance for a cryptocurrency</returns>
    public static Symbol Crypto(string ticker, string exchange) => new(ticker, exchange, AssetType.Crypto);

    /// <summary>
    /// Creates a Symbol for an ETF.
    /// </summary>
    /// <param name="ticker">The ETF ticker symbol</param>
    /// <param name="exchange">The exchange code</param>
    /// <returns>A Symbol instance for an ETF</returns>
    public static Symbol ETF(string ticker, string exchange) => new(ticker, exchange, AssetType.ETF);

    /// <summary>
    /// Gets the full symbol representation including exchange.
    /// </summary>
    /// <returns>A formatted string showing ticker, exchange, and asset type</returns>
    public string GetFullSymbol() => $"{Ticker}.{Exchange}";

    /// <summary>
    /// Determines whether this Symbol instance equals another Symbol instance.
    /// </summary>
    /// <param name="other">The Symbol instance to compare</param>
    /// <returns>True if ticker, exchange, and asset type are equal</returns>
    public bool Equals(Symbol? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Ticker == other.Ticker &&
               Exchange == other.Exchange &&
               AssetType == other.AssetType;
    }

    /// <summary>
    /// Determines whether this Symbol instance equals another object.
    /// </summary>
    /// <param name="obj">The object to compare</param>
    /// <returns>True if the object is a Symbol instance with equal properties</returns>
    public override bool Equals(object? obj) => Equals(obj as Symbol);

    /// <summary>
    /// Gets the hash code for this Symbol instance.
    /// </summary>
    /// <returns>A hash code based on ticker, exchange, and asset type</returns>
    public override int GetHashCode() => HashCode.Combine(Ticker, Exchange, AssetType);

    /// <summary>
    /// Returns a string representation of this Symbol instance.
    /// </summary>
    /// <returns>A formatted string showing ticker, exchange, and asset type</returns>
    public override string ToString() => $"{Ticker} ({Exchange}) - {AssetType}";

    /// <summary>
    /// Equality operator for Symbol instances.
    /// </summary>
    public static bool operator ==(Symbol? left, Symbol? right) => Equals(left, right);

    /// <summary>
    /// Inequality operator for Symbol instances.
    /// </summary>
    public static bool operator !=(Symbol? left, Symbol? right) => !Equals(left, right);
}
