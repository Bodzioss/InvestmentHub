namespace InvestmentHub.Contracts;

/// <summary>
/// Response DTO for Investment entity.
/// </summary>
public class InvestmentResponseDto
{
    /// <summary>Gets or sets the investment ID</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the portfolio ID</summary>
    public string PortfolioId { get; set; } = string.Empty;

    /// <summary>Gets or sets the symbol information</summary>
    public SymbolResponseDto Symbol { get; set; } = new();

    /// <summary>Gets or sets the current value</summary>
    public MoneyResponseDto CurrentValue { get; set; } = new();

    /// <summary>Gets or sets the purchase price</summary>
    public MoneyResponseDto PurchasePrice { get; set; } = new();

    /// <summary>Gets or sets the quantity</summary>
    public decimal Quantity { get; set; }

    /// <summary>Gets or sets the purchase date</summary>
    public DateTime PurchaseDate { get; set; }

    /// <summary>Gets or sets the last updated date</summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>Gets or sets the investment status</summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for Portfolio entity.
/// </summary>
public class PortfolioResponseDto
{
    /// <summary>Gets or sets the portfolio ID</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the owner ID</summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>Gets or sets the portfolio name</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the portfolio description</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the creation date</summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>Gets or sets the last updated date</summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>Gets or sets the total value</summary>
    public MoneyResponseDto? TotalValue { get; set; }

    /// <summary>Gets or sets the total cost</summary>
    public MoneyResponseDto? TotalCost { get; set; }

    /// <summary>Gets or sets the unrealized gain/loss</summary>
    public MoneyResponseDto? UnrealizedGainLoss { get; set; }

    /// <summary>Gets or sets the active investment count</summary>
    public int ActiveInvestmentCount { get; set; }

    /// <summary>Gets or sets the portfolio currency</summary>
    public string Currency { get; set; } = "USD";
}

/// <summary>
/// Response DTO for User entity.
/// </summary>
public class UserResponseDto
{
    /// <summary>Gets or sets the user ID</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the user name</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the user email</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the creation date</summary>
    public DateTime CreatedDate { get; set; }
}

/// <summary>
/// Response DTO for Symbol information.
/// </summary>
public class SymbolResponseDto
{
    /// <summary>Gets or sets the ticker symbol</summary>
    public string Ticker { get; set; } = string.Empty;

    /// <summary>Gets or sets the exchange</summary>
    public string Exchange { get; set; } = string.Empty;

    /// <summary>Gets or sets the asset type</summary>
    public string AssetType { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for Money information.
/// </summary>
public class MoneyResponseDto
{
    /// <summary>Gets or sets the amount</summary>
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the currency</summary>
    public string Currency { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for Market Price information.
/// </summary>
public class MarketPriceDto
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal? Open { get; set; }
    public decimal? High { get; set; }
    public decimal? Low { get; set; }
    public decimal? Close { get; set; }
    public long? Volume { get; set; }
    public DateTime Timestamp { get; set; }
    public string Source { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for Instrument entity.
/// </summary>
public class InstrumentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Ticker { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string Isin { get; set; } = string.Empty;
}
