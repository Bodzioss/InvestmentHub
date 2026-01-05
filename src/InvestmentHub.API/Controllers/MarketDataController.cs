using InvestmentHub.Contracts;
using InvestmentHub.Contracts.MarketData;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Interfaces;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Infrastructure.Data;
using InvestmentHub.Infrastructure.Jobs;
using InvestmentHub.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestmentHub.API.Controllers;

[ApiController]
[Route("api/market")]
public class MarketDataController : ControllerBase
{
    private readonly MarketPriceService _marketPriceService;
    private readonly ApplicationDbContext _dbContext;

    public MarketDataController(MarketPriceService marketPriceService, ApplicationDbContext dbContext)
    {
        _marketPriceService = marketPriceService;
        _dbContext = dbContext;
    }

    [HttpGet("price/{symbol}")]
    public async Task<ActionResult<MarketPriceDto>> GetPrice(string symbol)
    {
        var instrument = await _dbContext.Instruments.FirstOrDefaultAsync(i => i.Symbol.Ticker == symbol);
        var sym = instrument?.Symbol ?? new Symbol(symbol, "GPW", AssetType.Stock);

        var price = await _marketPriceService.GetCurrentPriceAsync(sym);

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
        var instrument = await _dbContext.Instruments.FirstOrDefaultAsync(i => i.Symbol.Ticker == symbol);
        var sym = instrument?.Symbol ?? new Symbol(symbol, "GPW", AssetType.Stock);

        var history = await _marketPriceService.GetHistoricalPricesAsync(
            sym,
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

    [HttpPost("trace/{symbol}")]
    public async Task<ActionResult<MarketPriceRefreshDto>> TracePrice(string symbol)
    {
        var instrument = await _dbContext.Instruments.FirstOrDefaultAsync(i => i.Symbol.Ticker == symbol);
        var sym = instrument?.Symbol ?? new Symbol(symbol, "GPW", AssetType.Stock);

        var (price, logs) = await _marketPriceService.ForceRefreshPriceWithLogsAsync(sym);

        return Ok(new MarketPriceRefreshDto
        {
            Symbol = symbol,
            Price = price?.Price,
            Currency = price?.Currency,
            Timestamp = price?.Timestamp,
            Source = price?.Source,
            TraceLogs = logs,
            Success = price != null
        });
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
