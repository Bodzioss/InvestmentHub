namespace InvestmentHub.Contracts.Transactions;

/// <summary>
/// Request to record a BUY transaction.
/// </summary>
public record RecordBuyRequest
{
    public required string Ticker { get; init; }
    public required string Exchange { get; init; }
    public required string AssetType { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal PricePerUnit { get; init; }
    public required string Currency { get; init; }
    public decimal? Fee { get; init; }
    public DateTime TransactionDate { get; init; } = DateTime.UtcNow;
    public DateTime? MaturityDate { get; init; } // For bonds
    public string? Notes { get; init; }
}

/// <summary>
/// Request to record a SELL transaction.
/// </summary>
public record RecordSellRequest
{
    public required string Ticker { get; init; }
    public required string Exchange { get; init; }
    public required string AssetType { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal SalePrice { get; init; }
    public required string Currency { get; init; }
    public decimal? Fee { get; init; }
    public DateTime TransactionDate { get; init; } = DateTime.UtcNow;
    public string? Notes { get; init; }
}

/// <summary>
/// Request to record a DIVIDEND payment.
/// </summary>
public record RecordDividendRequest
{
    public required string Ticker { get; init; }
    public required string Exchange { get; init; }
    public required decimal GrossAmount { get; init; }
    public required string Currency { get; init; }
    public DateTime PaymentDate { get; init; } = DateTime.UtcNow;
    public decimal? TaxRate { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Request to record an INTEREST payment.
/// </summary>
public record RecordInterestRequest
{
    public required string Ticker { get; init; }
    public required string Exchange { get; init; }
    public required decimal GrossAmount { get; init; }
    public required string Currency { get; init; }
    public DateTime PaymentDate { get; init; } = DateTime.UtcNow;
    public decimal? TaxRate { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Request to update a transaction.
/// </summary>
public record UpdateTransactionRequest
{
    public decimal? Quantity { get; init; }
    public decimal? PricePerUnit { get; init; }
    public decimal? Fee { get; init; }
    public decimal? GrossAmount { get; init; }
    public decimal? TaxRate { get; init; }
    public DateTime? TransactionDate { get; init; }
    public string? Notes { get; init; }
}
