using InvestmentHub.Domain.ReadModels;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Marten;

namespace InvestmentHub.API.Controllers;

[ApiController]
[Route("api/portfolios")]
[Authorize]
public class PortfolioActionsController : ControllerBase
{
    private readonly IDocumentSession _session;
    private readonly MarketPriceService _marketPriceService;
    private readonly ILogger<PortfolioActionsController> _logger;

    public PortfolioActionsController(
        IDocumentSession session,
        MarketPriceService marketPriceService,
        ILogger<PortfolioActionsController> logger)
    {
        _session = session;
        _marketPriceService = marketPriceService;
        _logger = logger;
    }

    /// <summary>
    /// Refreshes market prices for all investments in a portfolio.
    /// Forces fresh fetch from market data provider.
    /// </summary>
    [HttpPost("{portfolioId}/refresh-prices")]
    public async Task<IActionResult> RefreshPrices(string portfolioId)
    {
        try
        {
            _logger.LogInformation("Refreshing prices for portfolio {PortfolioId}", portfolioId);

            var portfolioGuid = Guid.Parse(portfolioId);

            // Get all investments for this portfolio from Marten READ MODEL
            var investments = await _session
                .Query<InvestmentReadModel>()
                .Where(i => i.PortfolioId == portfolioGuid)
                .ToListAsync();

            _logger.LogInformation("Marten returned {Count} total investments", investments.Count);

            var activeInvestments = investments.Where(i => i.Status == InvestmentStatus.Active).ToList();

            _logger.LogInformation("Found {Count} active investments to refresh", activeInvestments.Count);

            // Force refresh price for each unique symbol (use Ticker directly from read model)
            var symbols = activeInvestments
                .Select(i => i.Ticker)
                .Distinct()
                .ToList();

            var refreshedCount = 0;
            var updatedInvestments = new List<InvestmentReadModel>();

            foreach (var symbol in symbols)
            {
                try
                {
                    var price = await _marketPriceService.ForceRefreshPriceAsync(symbol, CancellationToken.None);
                    if (price != null)
                    {
                        refreshedCount++;
                        _logger.LogDebug("Refreshed price for {Symbol}: {Price} {Currency}",
                            symbol, price.Price, price.Currency);

                        // Update CurrentValue for all investments with this symbol
                        var investmentsToUpdate = activeInvestments.Where(i => i.Ticker == symbol);
                        foreach (var investment in investmentsToUpdate)
                        {
                            // Calculate new current value
                            var newCurrentValue = investment.Quantity * price.Price;

                            // Update the read model
                            investment.CurrentValue = newCurrentValue;
                            investment.Currency = price.Currency;
                            investment.LastUpdated = DateTime.UtcNow;

                            updatedInvestments.Add(investment);

                            _logger.LogDebug("Updated investment {Id} value to {Value} {Currency}",
                                investment.Id, newCurrentValue, price.Currency);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to refresh price for {Symbol}", symbol);
                }
            }

            // Save all updated read models back to Marten
            if (updatedInvestments.Any())
            {
                foreach (var investment in updatedInvestments)
                {
                    _session.Store(investment);
                }
                await _session.SaveChangesAsync();

                _logger.LogInformation("Saved {Count} updated investment read models", updatedInvestments.Count);
            }

            return Ok(new
            {
                message = $"Successfully refreshed {refreshedCount} of {symbols.Count} prices",
                refreshedCount,
                totalSymbols = symbols.Count,
                updatedInvestments = updatedInvestments.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing prices for portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, new { error = "Failed to refresh prices" });
        }
    }
}
