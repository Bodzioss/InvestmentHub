using InvestmentHub.Domain.ValueObjects;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace InvestmentHub.Domain.Commands;

/// <summary>
/// Command to record a SELL transaction for a portfolio.
/// </summary>
public record RecordSellTransactionCommand : IRequest<RecordSellTransactionResult>
{
    public PortfolioId PortfolioId { get; init; }
    public Symbol Symbol { get; init; }
    
    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be positive")]
    public decimal Quantity { get; init; }
    
    public Money SalePrice { get; init; }
    public Money? Fee { get; init; }
    
    [Required(ErrorMessage = "Transaction date is required")]
    public DateTime TransactionDate { get; init; }
    
    public string? Notes { get; init; }
    public TransactionId TransactionId { get; init; }

    public RecordSellTransactionCommand(
        PortfolioId portfolioId,
        Symbol symbol,
        decimal quantity,
        Money salePrice,
        DateTime transactionDate,
        Money? fee = null,
        string? notes = null)
    {
        PortfolioId = portfolioId;
        Symbol = symbol;
        Quantity = quantity;
        SalePrice = salePrice;
        TransactionDate = transactionDate;
        Fee = fee;
        Notes = notes;
        TransactionId = TransactionId.New();
    }
}

public record RecordSellTransactionResult
{
    public TransactionId TransactionId { get; init; } = null!;
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static RecordSellTransactionResult Success(TransactionId transactionId) =>
        new() { TransactionId = transactionId, IsSuccess = true };

    public static RecordSellTransactionResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
