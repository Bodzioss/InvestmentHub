using InvestmentHub.Contracts;
using InvestmentHub.Domain.Interfaces;
using InvestmentHub.Infrastructure.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace InvestmentHub.API.Controllers;

[ApiController]
[Route("api/market")]
public class MarketDataController : ControllerBase
{
    private readonly IMarketDataProvider _marketDataProvider;

    public MarketDataController(IMarketDataProvider marketDataProvider)
    {
        _marketDataProvider = marketDataProvider;
    }

    [HttpGet("price/{symbol}")]
    public async Task<ActionResult<MarketPriceDto>> GetPrice(string symbol)
    {
        var price = await _marketDataProvider.GetLatestPriceAsync(symbol);
        
        if (price == null)
            return NotFound();

        return Ok(new MarketPriceDto
        {
            Symbol = price.Symbol,
            Price = price.Price,
            Currency = price.Currency,
            Timestamp = price.Timestamp,
            Source = price.Source
        });
    }

    [HttpGet("history/{symbol}")]
    public async Task<ActionResult<IEnumerable<MarketPriceDto>>> GetHistory(string symbol, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var history = await _marketDataProvider.GetHistoricalPricesAsync(
            symbol, 
            from ?? DateTime.UtcNow.AddYears(-1), 
            to ?? DateTime.UtcNow);

        return Ok(history.Select(h => new MarketPriceDto
        {
            Symbol = h.Symbol,
            Price = h.Price,
            Open = h.Open,
            High = h.High,
            Low = h.Low,
            Close = h.Close,
            Volume = h.Volume,
            Timestamp = h.Timestamp,
            Source = h.Source
        }));
    }

    [HttpPost("import/{symbol}")]
    public async Task<IActionResult> ImportHistory(string symbol)
    {
        // Trigger background job logic directly or enqueue it
        // Since HistoricalImportJob is registered as scoped/transient, we can use it directly 
        // or better, enqueue via Hangfire if we want async processing.
        
        Hangfire.BackgroundJob.Enqueue<HistoricalImportJob>(job => job.ImportHistoryAsync(symbol));
        
        return Accepted(new { Message = $"Import started for {symbol}" });
    }
}
