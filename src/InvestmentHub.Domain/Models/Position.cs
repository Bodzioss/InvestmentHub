using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Models;

/// <summary>
/// Read model representing current position for a symbol from transaction history.
/// Calculated using FIFO cost basis.
/// </summary>
public class Position
{
    public Symbol Symbol { get; init; } = null!;
    public PortfolioId PortfolioId { get; init; } = null!;

    // Current holdings
    public decimal TotalQuantity { get; init; }

    // Cost basis (FIFO)
    public Money AverageCost { get; init; } = null!;
    public Money TotalCost { get; init; } = null!; // Total invested (AverageCost * Quantity)

    // Market value
    public Money CurrentPrice { get; init; } = null!;
    public Money CurrentValue { get; init; } = null!; // CurrentPrice * Quantity

    // P&L
    public Money UnrealizedGainLoss { get; init; } = null!;
    public decimal UnrealizedGainLossPercent { get; init; }

    // Income
    public Money TotalDividends { get; init; } = null!;
    public Money TotalInterest { get; init; } = null!;
    public Money TotalIncome { get; init; } = null!;

    // Realized gains (from sells using FIFO)
    public Money RealizedGainLoss { get; init; } = null!;

    // Bond specific
    public DateTime? MaturityDate { get; init; }
}
