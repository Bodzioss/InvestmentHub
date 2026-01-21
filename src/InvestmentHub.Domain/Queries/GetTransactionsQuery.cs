using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.ValueObjects;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace InvestmentHub.Domain.Queries;

/// <summary>
/// Query to get all transactions for a specific portfolio.
/// </summary>
public record GetTransactionsQuery : IRequest<GetTransactionsResult>
{
    [Required(ErrorMessage = "Portfolio ID is required")]
    public PortfolioId PortfolioId { get; init; }

    public TransactionType? TypeFilter { get; init; }
    public Symbol? SymbolFilter { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }

    [Range(1, 1000, ErrorMessage = "Limit must be between 1 and 1000")]
    public int Limit { get; init; } = 100;

    [Range(0, int.MaxValue, ErrorMessage = "Offset must be non-negative")]
    public int Offset { get; init; } = 0;

    public GetTransactionsQuery(
        PortfolioId portfolioId,
        TransactionType? typeFilter = null,
        Symbol? symbolFilter = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int limit = 100,
        int offset = 0)
    {
        PortfolioId = portfolioId;
        TypeFilter = typeFilter;
        SymbolFilter = symbolFilter;
        StartDate = startDate;
        EndDate = endDate;
        Limit = limit;
        Offset = offset;
    }
}

public record GetTransactionsResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<Transaction>? Transactions { get; init; }
    public int TotalCount { get; init; }

    public static GetTransactionsResult Success(IReadOnlyList<Transaction> transactions, int totalCount) =>
        new() { IsSuccess = true, Transactions = transactions, TotalCount = totalCount };

    public static GetTransactionsResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
