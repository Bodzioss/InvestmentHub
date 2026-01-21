namespace InvestmentHub.Contracts.Transactions;

/// <summary>
/// Response for a transaction operation.
/// </summary>
public record TransactionResponse
{
    public Guid TransactionId { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Ticker { get; init; } = string.Empty;
    public string Exchange { get; init; } = string.Empty;
    public string AssetType { get; init; } = string.Empty;
    public decimal? Quantity { get; init; }
    public decimal? PricePerUnit { get; init; }
    public string? Currency { get; init; }
    public decimal? Fee { get; init; }
    public decimal? GrossAmount { get; init; }
    public decimal? NetAmount { get; init; }
    public decimal? TaxRate { get; init; }
    public DateTime TransactionDate { get; init; }
    public DateTime? MaturityDate { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Response for a created transaction.
/// </summary>
public record TransactionCreatedResponse
{
    public Guid TransactionId { get; init; }
    public string Message { get; init; } = string.Empty;
    public decimal? NetAmount { get; init; } // For dividends/interest
}

/// <summary>
/// Response for portfolio transactions list.
/// </summary>
public record TransactionsListResponse
{
    public IReadOnlyList<TransactionResponse> Transactions { get; init; } = [];
    public int TotalCount { get; init; }
}
