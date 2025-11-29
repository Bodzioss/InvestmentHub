namespace InvestmentHub.Contracts.Messages;

/// <summary>
/// Message contract for when an investment is sold.
/// </summary>
public interface InvestmentSoldMessage
{
    Guid InvestmentId { get; }
    Guid PortfolioId { get; }
    decimal SalePriceAmount { get; }
    string SalePriceCurrency { get; }
    decimal QuantitySold { get; }
    DateTime SaleDate { get; }
    decimal PurchasePriceAmount { get; }
    string PurchasePriceCurrency { get; }
    decimal TotalProceedsAmount { get; }
    decimal TotalCostAmount { get; }
    decimal RealizedProfitLossAmount { get; }
    decimal ROIPercentage { get; }
    bool IsCompleteSale { get; }
}
