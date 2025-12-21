namespace InvestmentHub.Contracts.Income;

/// <summary>
/// Response for income summary.
/// </summary>
public record IncomeSummaryResponse
{
    public decimal TotalDividends { get; init; }
    public decimal TotalInterest { get; init; }
    public decimal TotalIncome { get; init; }
    public string Currency { get; init; } = "USD";
    public IReadOnlyList<IncomeBySymbol> BySymbol { get; init; } = [];
    public IReadOnlyList<IncomeByMonth> ByMonth { get; init; } = [];
}

/// <summary>
/// Income breakdown by symbol.
/// </summary>
public record IncomeBySymbol
{
    public string Ticker { get; init; } = string.Empty;
    public string Exchange { get; init; } = string.Empty;
    public decimal Dividends { get; init; }
    public decimal Interest { get; init; }
    public decimal Total { get; init; }
}

/// <summary>
/// Income breakdown by month.
/// </summary>
public record IncomeByMonth
{
    public int Year { get; init; }
    public int Month { get; init; }
    public decimal Dividends { get; init; }
    public decimal Interest { get; init; }
    public decimal Total { get; init; }
}
