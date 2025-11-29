namespace InvestmentHub.Contracts.Messages;

public interface InvestmentAddedMessage
{
    Guid PortfolioId { get; }
    Guid InvestmentId { get; }
    string Symbol { get; }
    decimal PurchasePrice { get; }
    string Currency { get; }
    decimal Quantity { get; }
    DateTime PurchaseDate { get; }
}
