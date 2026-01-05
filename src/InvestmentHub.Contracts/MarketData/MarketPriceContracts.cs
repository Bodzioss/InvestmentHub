using System;
using System.Collections.Generic;

namespace InvestmentHub.Contracts.MarketData;

public record MarketPriceRefreshDto
{
    public string Symbol { get; init; } = string.Empty;
    public decimal? Price { get; init; }
    public string? Currency { get; init; }
    public DateTime? Timestamp { get; init; }
    public string? Source { get; init; }
    public List<string> TraceLogs { get; init; } = new();
    public bool Success { get; init; }
}
