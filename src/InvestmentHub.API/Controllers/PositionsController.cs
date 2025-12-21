using MediatR;
using Microsoft.AspNetCore.Mvc;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Contracts.Positions;

namespace InvestmentHub.API.Controllers;

/// <summary>
/// Controller for viewing portfolio positions (read-only, calculated from transactions).
/// </summary>
[ApiController]
[Route("api/portfolios/{portfolioId:guid}/positions")]
public class PositionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PositionsController> _logger;

    public PositionsController(IMediator mediator, ILogger<PositionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets all positions for a portfolio (calculated using FIFO).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PositionsListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPositions(Guid portfolioId, [FromQuery] string? symbol = null)
    {
        Symbol? symbolFilter = null;
        if (!string.IsNullOrEmpty(symbol))
        {
            // TODO: Parse symbol filter if provided
        }

        var query = new GetPositionsQuery(new PortfolioId(portfolioId), symbolFilter);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        var positions = result.Positions!.Select(p => new PositionResponse
        {
            Ticker = p.Symbol?.Ticker ?? string.Empty,
            Exchange = p.Symbol?.Exchange ?? string.Empty,
            AssetType = p.Symbol?.AssetType.ToString() ?? string.Empty,
            TotalQuantity = p.TotalQuantity,
            AverageCost = p.AverageCost.Amount,
            TotalCost = p.TotalCost.Amount,
            CurrentPrice = p.CurrentPrice.Amount,
            CurrentValue = p.CurrentValue.Amount,
            Currency = p.TotalCost.Currency.ToString(),
            UnrealizedGainLoss = p.UnrealizedGainLoss.Amount,
            UnrealizedGainLossPercent = p.UnrealizedGainLossPercent,
            RealizedGainLoss = p.RealizedGainLoss.Amount,
            TotalDividends = p.TotalDividends.Amount,
            TotalInterest = p.TotalInterest.Amount,
            TotalIncome = p.TotalIncome.Amount,
            MaturityDate = p.MaturityDate
        }).ToList();

        var summary = new PositionsSummary
        {
            TotalValue = positions.Sum(p => p.CurrentValue),
            TotalCost = positions.Sum(p => p.TotalCost),
            TotalUnrealizedGainLoss = positions.Sum(p => p.UnrealizedGainLoss),
            TotalRealizedGainLoss = positions.Sum(p => p.RealizedGainLoss),
            TotalDividends = positions.Sum(p => p.TotalDividends),
            TotalInterest = positions.Sum(p => p.TotalInterest),
            Currency = positions.FirstOrDefault()?.Currency ?? "USD"
        };

        return Ok(new PositionsListResponse
        {
            Positions = positions,
            TotalCount = positions.Count,
            Summary = summary
        });
    }

    /// <summary>
    /// Gets position for a specific symbol.
    /// </summary>
    [HttpGet("{ticker}")]
    [ProducesResponseType(typeof(PositionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPositionBySymbol(Guid portfolioId, string ticker)
    {
        var query = new GetPositionsQuery(new PortfolioId(portfolioId), null);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        var position = result.Positions!.FirstOrDefault(p =>
            p.Symbol?.Ticker?.Equals(ticker, StringComparison.OrdinalIgnoreCase) == true);

        if (position == null)
            return NotFound(new { error = $"No position found for symbol {ticker}" });

        return Ok(new PositionResponse
        {
            Ticker = position.Symbol?.Ticker ?? string.Empty,
            Exchange = position.Symbol?.Exchange ?? string.Empty,
            AssetType = position.Symbol?.AssetType.ToString() ?? string.Empty,
            TotalQuantity = position.TotalQuantity,
            AverageCost = position.AverageCost.Amount,
            TotalCost = position.TotalCost.Amount,
            CurrentPrice = position.CurrentPrice.Amount,
            CurrentValue = position.CurrentValue.Amount,
            Currency = position.TotalCost.Currency.ToString(),
            UnrealizedGainLoss = position.UnrealizedGainLoss.Amount,
            UnrealizedGainLossPercent = position.UnrealizedGainLossPercent,
            RealizedGainLoss = position.RealizedGainLoss.Amount,
            TotalDividends = position.TotalDividends.Amount,
            TotalInterest = position.TotalInterest.Amount,
            TotalIncome = position.TotalIncome.Amount,
            MaturityDate = position.MaturityDate
        });
    }
}
