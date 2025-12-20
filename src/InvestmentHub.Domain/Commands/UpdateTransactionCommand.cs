using InvestmentHub.Domain.ValueObjects;
using MediatR;

namespace InvestmentHub.Domain.Commands;

/// <summary>
/// Command to update an existing transaction.
/// </summary>
public record UpdateTransactionCommand : IRequest<UpdateTransactionResult>
{
    public TransactionId TransactionId { get; init; }

    // Optional updates
    public decimal? Quantity { get; init; }
    public Money? PricePerUnit { get; init; }
    public Money? Fee { get; init; }
    public Money? GrossAmount { get; init; }
    public decimal? TaxRate { get; init; }
    public DateTime? TransactionDate { get; init; }
    public string? Notes { get; init; }

    public UpdateTransactionCommand(
        TransactionId transactionId,
        decimal? quantity = null,
        Money? pricePerUnit = null,
        Money? fee = null,
        Money? grossAmount = null,
        decimal? taxRate = null,
        DateTime? transactionDate = null,
        string? notes = null)
    {
        TransactionId = transactionId;
        Quantity = quantity;
        PricePerUnit = pricePerUnit;
        Fee = fee;
        GrossAmount = grossAmount;
        TaxRate = taxRate;
        TransactionDate = transactionDate;
        Notes = notes;
    }
}

public record UpdateTransactionResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static UpdateTransactionResult Success() =>
        new() { IsSuccess = true };

    public static UpdateTransactionResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
