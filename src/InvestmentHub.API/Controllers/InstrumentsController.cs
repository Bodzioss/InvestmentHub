using InvestmentHub.Contracts;
using InvestmentHub.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Enums;

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
    public async Task<ActionResult<object>> GetInstruments(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] string? exchange,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        // 1. Base query with eager loading
        var dbQuery = _context.Instruments
            .AsNoTracking()
            .Include(i => i.EtfDetails)
            .Include(i => i.BondDetails)
            .AsQueryable();

        // 2. Filter by AssetType
        if (!string.IsNullOrWhiteSpace(type) && type != "all")
        {
            dbQuery = dbQuery.Where(i => i.Symbol.AssetType.ToString() == type);
        }

        // 3. Filter by Exchange
        if (!string.IsNullOrWhiteSpace(exchange) && exchange != "all")
        {
            dbQuery = dbQuery.Where(i => i.Symbol.Exchange == exchange);
        }

        // 4. Search Filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedQuery = search.Trim().ToLower();
            dbQuery = dbQuery.Where(i =>
                i.Name.ToLower().Contains(normalizedQuery) ||
                i.Symbol.Ticker.ToLower().Contains(normalizedQuery) ||
                i.Isin.ToLower().Contains(normalizedQuery));
        }

        // 5. Get Total Count
        var totalCount = await dbQuery.CountAsync();

        // 6. Pagination
        var instruments = await dbQuery
            .OrderBy(i => i.Symbol.Ticker)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new InstrumentDto
            {
                Id = i.Id,
                Name = i.Name,
                Ticker = i.Symbol.Ticker,
                Exchange = i.Symbol.Exchange,
                AssetType = i.Symbol.AssetType.ToString(),
                Isin = i.Isin,
                EtfDetails = i.EtfDetails != null ? new EtfDetailsDto
                {
                    YearAdded = i.EtfDetails.YearAdded,
                    Region = i.EtfDetails.Region,
                    Theme = i.EtfDetails.Theme,
                    Manager = i.EtfDetails.Manager,
                    DistributionType = i.EtfDetails.DistributionType,
                    Domicile = i.EtfDetails.Domicile,
                    Replication = i.EtfDetails.Replication,
                    AnnualFeePercent = i.EtfDetails.AnnualFeePercent,
                    AssetsMillionsEur = i.EtfDetails.AssetsMillionsEur,
                    Currency = i.EtfDetails.Currency
                } : null,
                BondDetails = i.BondDetails != null ? new InvestmentHub.Contracts.BondDetailsDto
                {
                    BondType = i.BondDetails.Type.ToString(),
                    NominalValue = i.BondDetails.NominalValue,
                    InterestRate = i.BondDetails.FirstYearRate,
                    MaturityDate = i.BondDetails.MaturityDate,
                    Issuer = "Skarb Państwa" // Hardcoded for Treasury Bonds
                } : null
            })
            .ToListAsync();

        // 7. Return wrapped response
        return Ok(new
        {
            Instruments = instruments,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }
}
