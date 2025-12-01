using InvestmentHub.Contracts;
using InvestmentHub.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestmentHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InstrumentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public InstrumentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<InstrumentDto>>> GetInstruments(
        [FromQuery] string? query,
        [FromQuery] string? assetType,
        [FromQuery] string? exchange)
    {
        var dbQuery = _context.Instruments.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(assetType))
        {
            dbQuery = dbQuery.Where(i => i.Symbol.AssetType.ToString() == assetType);
        }

        if (!string.IsNullOrWhiteSpace(exchange))
        {
            dbQuery = dbQuery.Where(i => i.Symbol.Exchange == exchange);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalizedQuery = query.Trim().ToLower();
            dbQuery = dbQuery.Where(i => 
                i.Name.ToLower().Contains(normalizedQuery) || 
                i.Symbol.Ticker.ToLower().Contains(normalizedQuery) ||
                i.Isin.ToLower().Contains(normalizedQuery));
        }

        var instruments = await dbQuery
            .OrderBy(i => i.Symbol.Ticker)
            .Take(50) // Limit results
            .Select(i => new InstrumentDto
            {
                Id = i.Id,
                Name = i.Name,
                Ticker = i.Symbol.Ticker,
                Exchange = i.Symbol.Exchange,
                AssetType = i.Symbol.AssetType.ToString(),
                Isin = i.Isin
            })
            .ToListAsync();

        return Ok(instruments);
    }
}
