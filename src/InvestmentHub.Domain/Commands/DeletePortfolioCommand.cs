using InvestmentHub.Domain.ValueObjects;
using MediatR;

namespace InvestmentHub.Domain.Commands;

public record DeletePortfolioCommand(
    PortfolioId PortfolioId,
    UserId DeletedBy) : IRequest<DeletePortfolioResult>;

public record DeletePortfolioResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static DeletePortfolioResult Success() => new() { IsSuccess = true };
    public static DeletePortfolioResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}
