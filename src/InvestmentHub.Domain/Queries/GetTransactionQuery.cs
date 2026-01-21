using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.ValueObjects;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace InvestmentHub.Domain.Queries;

/// <summary>
/// Query to get a single transaction by ID.
/// </summary>
public record GetTransactionQuery : IRequest<GetTransactionResult>
{
    [Required(ErrorMessage = "Transaction ID is required")]
    public TransactionId TransactionId { get; init; }

    public GetTransactionQuery(TransactionId transactionId)
    {
        TransactionId = transactionId;
    }
}

public record GetTransactionResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public Transaction? Transaction { get; init; }

    public static GetTransactionResult Success(Transaction transaction) =>
        new() { IsSuccess = true, Transaction = transaction };

    public static GetTransactionResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
