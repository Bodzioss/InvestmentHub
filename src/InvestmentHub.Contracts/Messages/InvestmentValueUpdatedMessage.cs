namespace InvestmentHub.Contracts.Messages;

/// <summary>
/// Message contract for when an investment value is updated.
/// </summary>
public interface InvestmentValueUpdatedMessage
{
    Guid InvestmentId { get; }
    Guid PortfolioId { get; }
    decimal OldValueAmount { get; }
    string OldValueCurrency { get; }
    decimal NewValueAmount { get; }
    string NewValueCurrency { get; }
    DateTime UpdatedAt { get; }
    decimal ValueChangeAmount { get; }
    decimal PercentageChange { get; }
}
