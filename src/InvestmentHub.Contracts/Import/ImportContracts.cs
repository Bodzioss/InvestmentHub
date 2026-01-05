namespace InvestmentHub.Contracts.Import;

/// <summary>
/// Response for CSV import preview
/// </summary>
public record ImportPreviewResponse
{
    public List<ParsedTransactionDto> Transactions { get; init; } = new();
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public int TotalRows { get; init; }
    public int SkippedRows { get; init; }
    public int ParsedTransactions => Transactions.Count;
    public bool IsSuccess => Errors.Count == 0;
}

/// <summary>
/// DTO for a parsed transaction from CSV
/// </summary>
public record ParsedTransactionDto
{
    public DateTime Date { get; init; }
    public string OperationType { get; init; } = string.Empty;
    public string TransactionType { get; init; } = string.Empty;
    public string Account { get; init; } = string.Empty;
    public string Ticker { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal PricePerUnit { get; init; }
    public decimal TotalValue { get; init; }
    public string? Notes { get; init; }
    public bool InstrumentExists { get; init; }
    public string? AssetType { get; init; }
    public bool Selected { get; init; } = true;
}

/// <summary>
/// Request to import confirmed transactions
/// </summary>
public record ImportTransactionsRequest
{
    public required Guid PortfolioId { get; init; }
    public required string Exchange { get; init; }
    public required List<TransactionToImport> Transactions { get; init; }
}

/// <summary>
/// Single transaction to import
/// </summary>
public record TransactionToImport
{
    public DateTime Date { get; init; }
    public required string TransactionType { get; init; }
    public required string Ticker { get; init; }
    public required string Currency { get; init; }
    public decimal Quantity { get; init; }
    public decimal PricePerUnit { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Response after importing transactions
/// </summary>
public record ImportTransactionsResponse
{
    public int ImportedCount { get; init; }
    public int FailedCount { get; init; }
    public List<string> Errors { get; init; } = new();
    public bool IsSuccess => FailedCount == 0;
}
