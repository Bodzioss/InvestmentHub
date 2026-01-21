using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Infrastructure.Data;
using InvestmentHub.Infrastructure.TreasuryBonds;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestmentHub.API.Controllers;

/// <summary>
/// API controller for Polish Treasury Bonds (Obligacje Skarbowe).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BondsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly BondValueCalculator _calculator;
    private readonly BondDataProvider _dataProvider;
    private readonly ILogger<BondsController> _logger;

    public BondsController(
        ApplicationDbContext db,
        BondValueCalculator calculator,
        BondDataProvider dataProvider,
        ILogger<BondsController> logger)
    {
        _db = db;
        _calculator = calculator;
        _dataProvider = dataProvider;
        _logger = logger;
    }

    /// <summary>
    /// Syncs bond offerings from obligacjeskarbowe.pl to the database.
    /// </summary>
    [HttpPost("sync")]
    public async Task<ActionResult<SyncBondsResult>> SyncFromSource(CancellationToken ct)
    {
        try
        {
            var offerings = await _dataProvider.FetchCurrentOfferingsAsync(ct);
            var added = await _dataProvider.SyncBondsToDbAsync(offerings, ct);

            return Ok(new SyncBondsResult
            {
                FetchedCount = offerings.Count,
                AddedCount = added,
                Message = $"Fetched {offerings.Count} series, added {added} new to database"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync bonds from source");
            return StatusCode(500, new { error = "Failed to sync bonds", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets all available bond types with their characteristics.
    /// </summary>
    [HttpGet("types")]
    public ActionResult<IEnumerable<BondTypeInfo>> GetBondTypes()
    {
        var types = new List<BondTypeInfo>
        {
            new("OTS", "3-miesięczne", 0.25m, 0m, 0.50m, false),
            new("ROR", "Roczne", 1m, 0m, 0.50m, false),
            new("DOR", "2-letnie Rodzinne", 2m, 0m, 0.70m, false),
            new("TOS", "3-letnie", 3m, 0m, 0.70m, false),
            new("COI", "4-letnie indeksowane", 4m, 1.50m, 0.70m, true),
            new("ROS", "6-letnie Rodzinne", 6m, 1.00m, 2.00m, true),
            new("EDO", "10-letnie Emerytalne", 10m, 1.75m, 2.00m, true),
            new("ROD", "12-letnie Rodzinne", 12m, 2.00m, 2.00m, true)
        };

        return Ok(types);
    }

    /// <summary>
    /// Gets all available bond series from the data provider.
    /// </summary>
    [HttpGet("series")]
    public async Task<ActionResult<IEnumerable<BondSeriesDto>>> GetAvailableSeries(CancellationToken ct)
    {
        try
        {
            var offerings = await _dataProvider.FetchCurrentOfferingsAsync(ct);

            var series = offerings.Select(o => new BondSeriesDto
            {
                InstrumentId = Guid.Empty, // Will be populated after sync
                Symbol = o.Symbol,
                Name = GetBondName(o.Type, o.Symbol),
                Type = o.Type.ToString(),
                IssueDate = o.IssueDate,
                MaturityDate = o.MaturityDate,
                FirstYearRate = o.FirstYearRate,
                Margin = o.Margin,
                EarlyRedemptionFee = o.EarlyRedemptionFee
            }).OrderBy(s => s.Type).ThenBy(s => s.Symbol).ToList();

            return Ok(series);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get bond series");
            return StatusCode(500, new { error = "Failed to get bond series" });
        }
    }

    private static string GetBondName(BondType type, string symbol) => type switch
    {
        BondType.OTS => $"Obligacja 3-miesięczna {symbol}",
        BondType.ROR => $"Obligacja roczna {symbol}",
        BondType.DOR => $"Obligacja 2-letnia rodzinna {symbol}",
        BondType.TOS => $"Obligacja 3-letnia {symbol}",
        BondType.COI => $"Obligacja 4-letnia indeksowana {symbol}",
        BondType.ROS => $"Obligacja 6-letnia rodzinna {symbol}",
        BondType.EDO => $"Obligacja 10-letnia emerytalna {symbol}",
        BondType.ROD => $"Obligacja 12-letnia rodzinna {symbol}",
        _ => $"Obligacja {symbol}"
    };

    /// <summary>
    /// Gets detailed information about a specific bond series.
    /// </summary>
    [HttpGet("{symbol}")]
    public async Task<ActionResult<BondDetailsDto>> GetBondDetails(string symbol, CancellationToken ct)
    {
        var bond = await _db.Instruments
            .Include(i => i.BondDetails)
                .ThenInclude(b => b!.InterestPeriods)
            .FirstOrDefaultAsync(i => i.Symbol.Ticker == symbol && i.BondDetails != null, ct);

        if (bond?.BondDetails == null)
            return NotFound($"Bond series {symbol} not found");

        var dto = new BondDetailsDto
        {
            InstrumentId = bond.Id,
            Symbol = bond.Symbol.Ticker,
            Name = bond.Name,
            Type = bond.BondDetails.Type.ToString(),
            IssueDate = bond.BondDetails.IssueDate,
            MaturityDate = bond.BondDetails.MaturityDate,
            NominalValue = bond.BondDetails.NominalValue,
            FirstYearRate = bond.BondDetails.FirstYearRate,
            Margin = bond.BondDetails.Margin,
            EarlyRedemptionFee = bond.BondDetails.EarlyRedemptionFee,
            IsInflationIndexed = bond.BondDetails.IsInflationIndexed(),
            YearsToMaturity = bond.BondDetails.GetYearsToMaturity(),
            InterestPeriods = bond.BondDetails.InterestPeriods
                .OrderBy(p => p.PeriodNumber)
                .Select(p => new InterestPeriodDto
                {
                    PeriodNumber = p.PeriodNumber,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    InterestRate = p.InterestRate,
                    AccruedInterest = p.AccruedInterest
                })
                .ToList()
        };

        return Ok(dto);
    }

    /// <summary>
    /// Calculates the current or projected value of bonds.
    /// </summary>
    [HttpPost("calculate")]
    public async Task<ActionResult<BondValueResult>> CalculateValue(
        [FromBody] CalculateBondValueRequest request,
        CancellationToken ct)
    {
        var bond = await _db.Instruments
            .Include(i => i.BondDetails)
                .ThenInclude(b => b!.InterestPeriods)
            .FirstOrDefaultAsync(i => i.Symbol.Ticker == request.Symbol && i.BondDetails != null, ct);

        if (bond?.BondDetails == null)
            return NotFound($"Bond series {request.Symbol} not found");

        BondValueResult result;

        if (request.CalculationType == "early_redemption")
        {
            result = _calculator.CalculateEarlyRedemption(bond.BondDetails, request.Quantity, request.AsOfDate);
        }
        else if (request.CalculationType == "maturity")
        {
            result = _calculator.SimulateAtMaturity(
                bond.BondDetails,
                request.Quantity,
                request.AssumedInflation ?? 2.5m);
        }
        else
        {
            result = _calculator.Calculate(bond.BondDetails, request.Quantity, request.AsOfDate);
        }

        return Ok(result);
    }
}

// DTOs
public record BondTypeInfo(
    string Code,
    string Name,
    decimal DurationYears,
    decimal Margin,
    decimal EarlyRedemptionFee,
    bool IsInflationIndexed);

public class BondSeriesDto
{
    public Guid InstrumentId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime MaturityDate { get; set; }
    public decimal FirstYearRate { get; set; }
    public decimal Margin { get; set; }
    public decimal EarlyRedemptionFee { get; set; }
}

public class BondDetailsDto : BondSeriesDto
{
    public decimal NominalValue { get; set; }
    public bool IsInflationIndexed { get; set; }
    public int YearsToMaturity { get; set; }
    public List<InterestPeriodDto> InterestPeriods { get; set; } = new();
}

public class InterestPeriodDto
{
    public int PeriodNumber { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal InterestRate { get; set; }
    public decimal AccruedInterest { get; set; }
}

public class CalculateBondValueRequest
{
    public string Symbol { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public DateTime? AsOfDate { get; set; }
    public string CalculationType { get; set; } = "current"; // current, early_redemption, maturity
    public decimal? AssumedInflation { get; set; }
}

public record SyncBondsResult
{
    public int FetchedCount { get; init; }
    public int AddedCount { get; init; }
    public string Message { get; init; } = string.Empty;
}

