using InvestmentHub.Domain.Models;
using InvestmentHub.Domain.ValueObjects;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace InvestmentHub.Domain.Queries;

/// <summary>
/// Query to get all current positions for a portfolio.
/// Positions are calculated from transaction history using FIFO.
/// </summary>
public record GetPositionsQuery : IRequest<GetPositionsResult>
{
    [Required(ErrorMessage = "Portfolio ID is required")]
    public PortfolioId PortfolioId { get; init; }

    public Symbol? SymbolFilter { get; init; }

    public GetPositionsQuery(PortfolioId portfolioId, Symbol? symbolFilter = null)
    {
        PortfolioId = portfolioId;
        SymbolFilter = symbolFilter;
    }
}

public record GetPositionsResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<Position>? Positions { get; init; }

    public static GetPositionsResult Success(IReadOnlyList<Position> positions) =>
        new() { IsSuccess = true, Positions = positions };

    public static GetPositionsResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
