using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;

namespace InvestmentHub.API.Controllers;

[ApiController]
[Route("api/portfolios")]
[Authorize]
public class PortfolioActionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly MarketPriceService _marketPriceService;
    private readonly ILogger<PortfolioActionsController> _logger;

    public PortfolioActionsController(
        IMediator mediator,
        MarketPriceService marketPriceService,
        ILogger<PortfolioActionsController> logger)
    {
        _mediator = mediator;
        _marketPriceService = marketPriceService;
        _logger = logger;
    }

    /// <summary>
    /// Refreshes market prices for all ACTIVE positions in a portfolio.
    /// Only refreshes symbols where net quantity > 0.
    /// </summary>
    [HttpPost("{portfolioId}/refresh-prices")]
    public async Task<IActionResult> RefreshPrices(string portfolioId)
    {
        try
        {
            _logger.LogInformation("Refreshing prices for portfolio {PortfolioId}", portfolioId);

            // Use GetPositionsQuery to get only active positions (net quantity > 0)
            var portfolioIdVO = PortfolioId.FromString(portfolioId);
            var positionsResult = await _mediator.Send(new GetPositionsQuery(portfolioIdVO));

            if (!positionsResult.IsSuccess || positionsResult.Positions == null)
            {
                return BadRequest(new { error = positionsResult.ErrorMessage ?? "Failed to get positions" });
            }

            var positions = positionsResult.Positions.ToList();
            _logger.LogInformation("Found {Count} active positions to refresh", positions.Count);

            // Debug: log actual symbols found
            foreach (var p in positions)
            {
                _logger.LogInformation("  Position: {Ticker} ({Exchange}, {AssetType}), Qty: {Qty}",
                    p.Symbol.Ticker, p.Symbol.Exchange, p.Symbol.AssetType, p.TotalQuantity);
            }

            var refreshedCount = 0;

            foreach (var position in positions)
            {
                try
                {
                    var price = await _marketPriceService.ForceRefreshPriceAsync(position.Symbol, CancellationToken.None);
                    if (price != null)
                    {
                        refreshedCount++;
                        _logger.LogDebug("Refreshed price for {Symbol}: {Price} {Currency}",
                            position.Symbol.Ticker, price.Price, price.Currency);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to refresh price for {Symbol}", position.Symbol.Ticker);
                }
            }

            return Ok(new
            {
                message = $"Successfully refreshed {refreshedCount} of {positions.Count} prices",
                refreshedCount,
                totalSymbols = positions.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing prices for portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, new { error = "Failed to refresh prices" });
        }
    }
}
