namespace InvestmentHub.Contracts.Positions;

/// <summary>
/// Response for a single portfolio position.
/// </summary>
public record PositionResponse
{
    public string Ticker { get; init; } = string.Empty;
    public string Exchange { get; init; } = string.Empty;
    public string AssetType { get; init; } = string.Empty;
    public decimal TotalQuantity { get; init; }
    public decimal AverageCost { get; init; }
    public decimal TotalCost { get; init; }
    public decimal CurrentPrice { get; init; }
    public decimal CurrentValue { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal UnrealizedGainLoss { get; init; }
    public decimal UnrealizedGainLossPercent { get; init; }
    public decimal RealizedGainLoss { get; init; }
    public decimal TotalDividends { get; init; }
    public decimal TotalInterest { get; init; }
    public decimal TotalIncome { get; init; }
    public DateTime? MaturityDate { get; init; }
}

/// <summary>
/// Response for portfolio positions list.
/// </summary>
public record PositionsListResponse
{
    public IReadOnlyList<PositionResponse> Positions { get; init; } = [];
    public int TotalCount { get; init; }
    public PositionsSummary Summary { get; init; } = new();
}

/// <summary>
/// Summary totals for all positions.
/// </summary>
public record PositionsSummary
{
    public decimal TotalValue { get; init; }
    public decimal TotalCost { get; init; }
    public decimal TotalUnrealizedGainLoss { get; init; }
    public decimal TotalRealizedGainLoss { get; init; }
    public decimal TotalDividends { get; init; }
    public decimal TotalInterest { get; init; }
    public string Currency { get; init; } = "USD";
}
