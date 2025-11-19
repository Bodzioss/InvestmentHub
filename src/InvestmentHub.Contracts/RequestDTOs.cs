namespace InvestmentHub.Contracts;

/// <summary>
/// Request DTO for adding a new investment.
/// </summary>
public record AddInvestmentRequest
{
    /// <summary>Gets the portfolio ID</summary>
    public required string PortfolioId { get; init; }

    /// <summary>Gets the symbol information</summary>
    public required SymbolRequest Symbol { get; init; }

    /// <summary>Gets the purchase price</summary>
    public required MoneyRequest PurchasePrice { get; init; }

    /// <summary>Gets the quantity</summary>
    public required decimal Quantity { get; init; }

    /// <summary>Gets the purchase date</summary>
    public required DateTime PurchaseDate { get; init; }
}

/// <summary>
/// Request DTO for updating an investment.
/// </summary>
public record UpdateInvestmentRequest
{
    /// <summary>Gets the investment ID</summary>
    public required string InvestmentId { get; init; }

    /// <summary>Gets the new quantity</summary>
    public decimal? Quantity { get; init; }

    /// <summary>Gets the new purchase price</summary>
    public MoneyRequest? PurchasePrice { get; init; }
}

/// <summary>
/// Request DTO for updating investment value.
/// </summary>
public record UpdateInvestmentValueRequest
{
    /// <summary>Gets the investment ID</summary>
    public required string InvestmentId { get; init; }

    /// <summary>Gets the current price</summary>
    public required MoneyRequest CurrentPrice { get; init; }
}

/// <summary>
/// Request DTO for selling an investment.
/// </summary>
public record SellInvestmentRequest
{
    /// <summary>Gets the investment ID</summary>
    public required string InvestmentId { get; init; }

    /// <summary>Gets the sale price per unit</summary>
    public required MoneyRequest SalePrice { get; init; }

    /// <summary>Gets the quantity to sell (null = sell all remaining units)</summary>
    public decimal? QuantityToSell { get; init; }

    /// <summary>Gets the sale date</summary>
    public required DateTime SaleDate { get; init; }
}

/// <summary>
/// Request DTO for creating a portfolio.
/// </summary>
public record CreatePortfolioRequest
{
    /// <summary>Gets the portfolio name</summary>
    public required string Name { get; init; }

    /// <summary>Gets the portfolio description</summary>
    public string? Description { get; init; }

    /// <summary>Gets the owner ID</summary>
    public required string OwnerId { get; init; }
}

/// <summary>
/// Request DTO for updating a portfolio.
/// </summary>
public record UpdatePortfolioRequest
{
    /// <summary>Gets the portfolio ID</summary>
    public required string PortfolioId { get; init; }

    /// <summary>Gets the new name</summary>
    public string? Name { get; init; }

    /// <summary>Gets the new description</summary>
    public string? Description { get; init; }
}

/// <summary>
/// Request DTO for symbol information.
/// </summary>
public record SymbolRequest
{
    /// <summary>Gets the ticker symbol</summary>
    public required string Ticker { get; init; }

    /// <summary>Gets the exchange</summary>
    public required string Exchange { get; init; }

    /// <summary>Gets the asset type</summary>
    public required string AssetType { get; init; }
}

/// <summary>
/// Request DTO for money information.
/// </summary>
public record MoneyRequest
{
    /// <summary>Gets the amount</summary>
    public required decimal Amount { get; init; }

    /// <summary>Gets the currency</summary>
    public required string Currency { get; init; }
}

