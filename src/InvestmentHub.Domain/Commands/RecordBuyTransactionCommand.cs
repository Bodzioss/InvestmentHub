using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.ValueObjects;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace InvestmentHub.Domain.Commands;

/// <summary>
/// Command to record a BUY transaction for a portfolio.
/// </summary>
public record RecordBuyTransactionCommand : IRequest<RecordBuyTransactionResult>
{
    public PortfolioId PortfolioId { get; init; }
    public Symbol Symbol { get; init; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be positive")]
    public decimal Quantity { get; init; }

    public Money PricePerUnit { get; init; }
    public Money? Fee { get; init; }
    public DateTime? MaturityDate { get; init; } // For bonds

    [Required(ErrorMessage = "Transaction date is required")]
    public DateTime TransactionDate { get; init; }

    public string? Notes { get; init; }
    public TransactionId TransactionId { get; init; }

    public RecordBuyTransactionCommand(
        PortfolioId portfolioId,
        Symbol symbol,
        decimal quantity,
        Money pricePerUnit,
        DateTime transactionDate,
        Money? fee = null,
        DateTime? maturityDate = null,
        string? notes = null)
    {
        PortfolioId = portfolioId;
        Symbol = symbol;
        Quantity = quantity;
        PricePerUnit = pricePerUnit;
        TransactionDate = transactionDate;
        Fee = fee;
        MaturityDate = maturityDate;
        Notes = notes;
        TransactionId = TransactionId.New();
    }
}

public record RecordBuyTransactionResult
{
    public TransactionId TransactionId { get; init; } = null!;
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static RecordBuyTransactionResult Success(TransactionId transactionId) =>
        new() { TransactionId = transactionId, IsSuccess = true };

    public static RecordBuyTransactionResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
