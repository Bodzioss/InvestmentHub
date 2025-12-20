using InvestmentHub.Domain.ValueObjects;
using MediatR;

namespace InvestmentHub.Domain.Commands;

/// <summary>
/// Command to cancel (soft delete) a transaction.
/// </summary>
public record CancelTransactionCommand : IRequest<CancelTransactionResult>
{
    public TransactionId TransactionId { get; init; }

    public CancelTransactionCommand(TransactionId transactionId)
    {
        TransactionId = transactionId;
    }
}

public record CancelTransactionResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static CancelTransactionResult Success() =>
        new() { IsSuccess = true };

    public static CancelTransactionResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
