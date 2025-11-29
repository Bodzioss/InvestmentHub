using InvestmentHub.Domain.Interfaces;
using InvestmentHub.Infrastructure.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace InvestmentHub.API.Controllers;

[ApiController]
[Route("api/market")]
public class MarketDataController : ControllerBase
{
    private readonly IMarketDataProvider _marketDataProvider;
    private readonly HistoricalImportJob _historicalImportJob;

    public MarketDataController(
        IMarketDataProvider marketDataProvider,
        HistoricalImportJob historicalImportJob)
    {
        _marketDataProvider = marketDataProvider;
        _historicalImportJob = historicalImportJob;
    }

    [HttpGet("price/{symbol}")]
    public async Task<IActionResult> GetPrice(string symbol)
    {
        var price = await _marketDataProvider.GetLatestPriceAsync(symbol);
        if (price == null)
        {
            return NotFound($"Price not found for symbol {symbol}");
        }
        return Ok(price);
    }

    [HttpGet("history/{symbol}")]
    public async Task<IActionResult> GetHistory(string symbol, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var fromDate = from ?? DateTime.UtcNow.AddYears(-1);
        var toDate = to ?? DateTime.UtcNow;

        var history = await _marketDataProvider.GetHistoricalPricesAsync(symbol, fromDate, toDate);
        return Ok(history);
    }

    [HttpPost("import/{symbol}")]
    public async Task<IActionResult> ImportHistory(string symbol)
    {
        // Trigger background job logic directly or enqueue it
        // Since HistoricalImportJob is registered as scoped/transient, we can use it directly 
        // or better, enqueue via Hangfire if we want async processing.
        // For now, let's enqueue it to avoid blocking.
        
        Hangfire.BackgroundJob.Enqueue<HistoricalImportJob>(job => job.ImportHistoryAsync(symbol));
        
        return Accepted(new { Message = $"Import started for {symbol}" });
    }
}
