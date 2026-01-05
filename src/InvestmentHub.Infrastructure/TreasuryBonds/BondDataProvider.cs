using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.TreasuryBonds;

/// <summary>
/// Provides bond data - all active Polish Treasury Bonds as of January 2026.
/// Data sourced from obligacjeskarbowe.pl/tabela-odsetkowa/
/// </summary>
public class BondDataProvider
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<BondDataProvider> _logger;

    public BondDataProvider(
        ApplicationDbContext db,
        ILogger<BondDataProvider> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Gets all active bond series from obligacjeskarbowe.pl.
    /// </summary>
    public Task<List<BondSeriesInfo>> FetchCurrentOfferingsAsync(CancellationToken ct = default)
    {
        var offerings = new List<BondSeriesInfo>();

        // All active bond symbols extracted from obligacjeskarbowe.pl/tabela-odsetkowa/
        // COI - 4-letnie indeksowane
        var coiSymbols = new[] { "COI0126", "COI0127", "COI0128", "COI0129", "COI0130", "COI0226", "COI0227", "COI0228", "COI0229", "COI0326", "COI0327", "COI0328", "COI0329", "COI0426", "COI0427", "COI0428", "COI0429", "COI0526", "COI0527", "COI0528", "COI0529", "COI0626", "COI0627", "COI0628", "COI0629", "COI0726", "COI0727", "COI0728", "COI0729", "COI0826", "COI0827", "COI0828", "COI0829", "COI0926", "COI0927", "COI0928", "COI0929", "COI1026", "COI1027", "COI1028", "COI1029", "COI1126", "COI1127", "COI1128", "COI1129", "COI1225", "COI1226", "COI1227", "COI1228", "COI1229" };

        // DOR - 2-letnie rodzinne
        var dorSymbols = new[] { "DOR0126", "DOR0127", "DOR0128", "DOR0226", "DOR0227", "DOR0326", "DOR0327", "DOR0426", "DOR0427", "DOR0526", "DOR0527", "DOR0626", "DOR0627", "DOR0726", "DOR0727", "DOR0826", "DOR0827", "DOR0926", "DOR0927", "DOR1026", "DOR1027", "DOR1126", "DOR1127", "DOR1225", "DOR1226", "DOR1227" };

        // EDO - 10-letnie emerytalne
        var edoSymbols = new[] { "EDO0126", "EDO0127", "EDO0128", "EDO0129", "EDO0130", "EDO0131", "EDO0132", "EDO0133", "EDO0134", "EDO0135", "EDO0136", "EDO0226", "EDO0227", "EDO0228", "EDO0229", "EDO0230", "EDO0231", "EDO0232", "EDO0233", "EDO0234", "EDO0235", "EDO0326", "EDO0327", "EDO0328", "EDO0329", "EDO0330", "EDO0331", "EDO0332", "EDO0333", "EDO0334", "EDO0335", "EDO0426", "EDO0427", "EDO0428", "EDO0429", "EDO0430", "EDO0431", "EDO0432", "EDO0433", "EDO0434", "EDO0435", "EDO0526", "EDO0527", "EDO0528", "EDO0529", "EDO0530", "EDO0531", "EDO0532", "EDO0533", "EDO0534", "EDO0535", "EDO0626", "EDO0627", "EDO0628", "EDO0629", "EDO0630", "EDO0631", "EDO0632", "EDO0633", "EDO0634", "EDO0635", "EDO0726", "EDO0727", "EDO0728", "EDO0729", "EDO0730", "EDO0731", "EDO0732", "EDO0733", "EDO0734", "EDO0735", "EDO0826", "EDO0827", "EDO0828", "EDO0829", "EDO0830", "EDO0831", "EDO0832", "EDO0833", "EDO0834", "EDO0835", "EDO0926", "EDO0927", "EDO0928", "EDO0929", "EDO0930", "EDO0931", "EDO0932", "EDO0933", "EDO0934", "EDO0935", "EDO1026", "EDO1027", "EDO1028", "EDO1029", "EDO1030", "EDO1031", "EDO1032", "EDO1033", "EDO1034", "EDO1035", "EDO1126", "EDO1127", "EDO1128", "EDO1129", "EDO1130", "EDO1131", "EDO1132", "EDO1133", "EDO1134", "EDO1135", "EDO1225", "EDO1226", "EDO1227", "EDO1228", "EDO1229", "EDO1230", "EDO1231", "EDO1232", "EDO1233", "EDO1234", "EDO1235" };

        // OTS - 3-miesięczne
        var otsSymbols = new[] { "OTS0126", "OTS0226", "OTS0326", "OTS0426", "OTS1225" };

        // ROD - 12-letnie rodzinne
        var rodSymbols = new[] { "ROD0129", "ROD0130", "ROD0131", "ROD0132", "ROD0133", "ROD0134", "ROD0135", "ROD0136", "ROD0137", "ROD0138", "ROD0229", "ROD0230", "ROD0231", "ROD0232", "ROD0233", "ROD0234", "ROD0235", "ROD0236", "ROD0237", "ROD0329", "ROD0330", "ROD0331", "ROD0332", "ROD0333", "ROD0334", "ROD0335", "ROD0336", "ROD0337", "ROD0429", "ROD0430", "ROD0431", "ROD0432", "ROD0433", "ROD0434", "ROD0435", "ROD0436", "ROD0437", "ROD0529", "ROD0530", "ROD0531", "ROD0532", "ROD0533", "ROD0534", "ROD0535", "ROD0536", "ROD0537", "ROD0629", "ROD0630", "ROD0631", "ROD0632", "ROD0633", "ROD0634", "ROD0635", "ROD0636", "ROD0637", "ROD0729", "ROD0730", "ROD0731", "ROD0732", "ROD0733", "ROD0734", "ROD0735", "ROD0736", "ROD0737", "ROD0829", "ROD0830", "ROD0831", "ROD0832", "ROD0833", "ROD0834", "ROD0835", "ROD0836", "ROD0837", "ROD0929", "ROD0930", "ROD0931", "ROD0932", "ROD0933", "ROD0934", "ROD0935", "ROD0936", "ROD0937", "ROD1028", "ROD1029", "ROD1030", "ROD1031", "ROD1032", "ROD1033", "ROD1034", "ROD1035", "ROD1036", "ROD1037", "ROD1128", "ROD1129", "ROD1130", "ROD1131", "ROD1132", "ROD1133", "ROD1134", "ROD1135", "ROD1136", "ROD1137", "ROD1228", "ROD1229", "ROD1230", "ROD1231", "ROD1232", "ROD1233", "ROD1234", "ROD1235", "ROD1236", "ROD1237" };

        // ROR - roczne
        var rorSymbols = new[] { "ROR0126", "ROR0127", "ROR0226", "ROR0326", "ROR0426", "ROR0526", "ROR0626", "ROR0726", "ROR0826", "ROR0926", "ROR1026", "ROR1126", "ROR1225", "ROR1226" };

        // ROS - 6-letnie rodzinne
        var rosSymbols = new[] { "ROS0126", "ROS0127", "ROS0128", "ROS0129", "ROS0130", "ROS0131", "ROS0132", "ROS0226", "ROS0227", "ROS0228", "ROS0229", "ROS0230", "ROS0231", "ROS0326", "ROS0327", "ROS0328", "ROS0329", "ROS0330", "ROS0331", "ROS0426", "ROS0427", "ROS0428", "ROS0429", "ROS0430", "ROS0431", "ROS0526", "ROS0527", "ROS0528", "ROS0529", "ROS0530", "ROS0531", "ROS0626", "ROS0627", "ROS0628", "ROS0629", "ROS0630", "ROS0631", "ROS0726", "ROS0727", "ROS0728", "ROS0729", "ROS0730", "ROS0731", "ROS0825", "ROS0826", "ROS0827", "ROS0828", "ROS0829", "ROS0830", "ROS0831", "ROS0926", "ROS0927", "ROS0928", "ROS0929", "ROS0930", "ROS0931", "ROS1026", "ROS1027", "ROS1028", "ROS1029", "ROS1030", "ROS1031", "ROS1126", "ROS1127", "ROS1128", "ROS1129", "ROS1130", "ROS1131", "ROS1225", "ROS1226", "ROS1227", "ROS1228", "ROS1229", "ROS1230", "ROS1231" };

        // TOS - 3-letnie
        var tosSymbols = new[] { "TOS0126", "TOS0127", "TOS0128", "TOS0129", "TOS0226", "TOS0227", "TOS0228", "TOS0326", "TOS0327", "TOS0328", "TOS0426", "TOS0427", "TOS0428", "TOS0526", "TOS0527", "TOS0528", "TOS0626", "TOS0627", "TOS0628", "TOS0726", "TOS0727", "TOS0728", "TOS0826", "TOS0827", "TOS0828", "TOS0926", "TOS0927", "TOS0928", "TOS1026", "TOS1027", "TOS1028", "TOS1126", "TOS1127", "TOS1128", "TOS1225", "TOS1226", "TOS1227", "TOS1228" };

        // Create bond info for each type
        foreach (var symbol in otsSymbols) offerings.Add(CreateBondInfo(symbol, BondType.OTS, 0.25m, 0m, 0.50m, 3.00m));
        foreach (var symbol in rorSymbols) offerings.Add(CreateBondInfo(symbol, BondType.ROR, 1m, 0m, 0.50m, 5.70m));
        foreach (var symbol in dorSymbols) offerings.Add(CreateBondInfo(symbol, BondType.DOR, 2m, 0m, 0.70m, 5.80m));
        foreach (var symbol in tosSymbols) offerings.Add(CreateBondInfo(symbol, BondType.TOS, 3m, 0m, 0.70m, 5.85m));
        foreach (var symbol in coiSymbols) offerings.Add(CreateBondInfo(symbol, BondType.COI, 4m, 1.50m, 0.70m, 5.00m));
        foreach (var symbol in rosSymbols) offerings.Add(CreateBondInfo(symbol, BondType.ROS, 6m, 1.00m, 2.00m, 5.15m));
        foreach (var symbol in edoSymbols) offerings.Add(CreateBondInfo(symbol, BondType.EDO, 10m, 1.75m, 2.00m, 5.40m));
        foreach (var symbol in rodSymbols) offerings.Add(CreateBondInfo(symbol, BondType.ROD, 12m, 2.00m, 2.00m, 5.50m));

        return Task.FromResult(offerings);
    }

    private BondSeriesInfo CreateBondInfo(string symbol, BondType type, decimal durationYears, decimal margin, decimal earlyRedemptionFee, decimal firstYearRate)
    {
        // Parse maturity date from symbol (e.g., EDO0136 -> January 2036)
        var monthStr = symbol.Substring(3, 2);
        var yearStr = symbol.Substring(5, 2);
        var month = int.Parse(monthStr);
        var year = 2000 + int.Parse(yearStr);

        var maturityDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var issueDate = maturityDate.AddYears(-(int)durationYears);

        // Adjust for fractional years (OTS = 3 months)
        if (durationYears < 1)
        {
            issueDate = maturityDate.AddMonths(-(int)(durationYears * 12));
        }

        return new BondSeriesInfo
        {
            Symbol = symbol,
            Type = type,
            IssueDate = issueDate,
            MaturityDate = maturityDate,
            FirstYearRate = firstYearRate,
            Margin = margin,
            EarlyRedemptionFee = earlyRedemptionFee,
            NominalValue = 100m
        };
    }

    /// <summary>
    /// Creates or updates bond instruments in the database.
    /// </summary>
    public async Task<int> SyncBondsToDbAsync(List<BondSeriesInfo> offerings, CancellationToken ct = default)
    {
        var count = 0;

        foreach (var offering in offerings)
        {
            var existingInstrument = await _db.Instruments
                .Include(i => i.BondDetails)
                .FirstOrDefaultAsync(i => i.Symbol.Ticker == offering.Symbol, ct);

            if (existingInstrument == null)
            {
                var instrument = new Instrument(
                    new Symbol(offering.Symbol, "PKO", AssetType.Bond),
                    GetBondName(offering.Type, offering.Symbol),
                    $"PL{offering.Symbol}"
                );

                _db.Instruments.Add(instrument);
                await _db.SaveChangesAsync(ct);

                var bondDetails = new TreasuryBondDetails(
                    instrument.Id,
                    offering.Type,
                    offering.IssueDate,
                    offering.MaturityDate,
                    offering.NominalValue,
                    offering.FirstYearRate,
                    offering.Margin,
                    offering.EarlyRedemptionFee
                );

                _db.TreasuryBondDetails.Add(bondDetails);
                await _db.SaveChangesAsync(ct);
                count++;

                _logger.LogDebug("Added bond series: {Symbol}", offering.Symbol);
            }
        }

        if (count > 0)
        {
            _logger.LogInformation("Added {Count} new bond series to database", count);
        }

        return count;
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
}

/// <summary>
/// Information about a bond series offering.
/// </summary>
public class BondSeriesInfo
{
    public string Symbol { get; set; } = string.Empty;
    public BondType Type { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime MaturityDate { get; set; }
    public decimal FirstYearRate { get; set; }
    public decimal Margin { get; set; }
    public decimal EarlyRedemptionFee { get; set; }
    public decimal NominalValue { get; set; }
}
