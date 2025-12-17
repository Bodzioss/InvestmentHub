namespace InvestmentHub.Domain.Entities;

/// <summary>
/// Represents a cached market price fetched from external provider.
/// Prevents redundant API calls by storing prices with timestamps.
/// </summary>
public class CachedMarketPrice
{
    public Guid Id { get; set; }

    /// <summary>
    /// The ticker symbol (e.g., "AAPL", "MSFT")
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// The price at time of fetch
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Currency of the price (e.g., "USD", "EUR")
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// When this price was fetched from the external provider
    /// </summary>
    public DateTime FetchedAt { get; set; }

    /// <summary>
    /// Source of the price data (e.g., "Yahoo", "Alpha Vantage")
    /// </summary>
    public string Source { get; set; } = "Yahoo";
}
