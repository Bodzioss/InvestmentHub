using System;

namespace InvestmentHub.Domain.Entities;

/// <summary>
/// Stores detailed information specific to ETF instruments.
/// Data sourced from ETF tracking lists and fund providers.
/// </summary>
public class EtfDetails
{
    /// <summary>
    /// Unique identifier for the ETF details record.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Foreign key to the parent Instrument.
    /// </summary>
    public Guid InstrumentId { get; private set; }

    /// <summary>
    /// Year the ETF was added to the tracking list.
    /// </summary>
    public int? YearAdded { get; private set; }

    /// <summary>
    /// Investment region or country (e.g., "Europa", "USA", "Globalny").
    /// </summary>
    public string? Region { get; private set; }

    /// <summary>
    /// Investment theme (e.g., "Akcje", "Obligacje", "Surowce").
    /// </summary>
    public string? Theme { get; private set; }

    /// <summary>
    /// Fund manager/provider (e.g., "iShares", "Vanguard", "Xtrackers").
    /// </summary>
    public string? Manager { get; private set; }

    /// <summary>
    /// Distribution type: "Accumulating" or "Distributing".
    /// </summary>
    public string? DistributionType { get; private set; }

    /// <summary>
    /// Fund domicile country (e.g., "Irlandia", "Luksemburg").
    /// </summary>
    public string? Domicile { get; private set; }

    /// <summary>
    /// Replication method: "Fizyczna" (Physical) or "Synt." (Synthetic).
    /// </summary>
    public string? Replication { get; private set; }

    /// <summary>
    /// Annual expense ratio as percentage (e.g., 0.20 for 0.20%).
    /// </summary>
    public decimal? AnnualFeePercent { get; private set; }

    /// <summary>
    /// Assets under management in millions of EUR.
    /// </summary>
    public decimal? AssetsMillionsEur { get; private set; }

    /// <summary>
    /// Fund base currency (e.g., "USD", "EUR").
    /// </summary>
    public string? Currency { get; private set; }

    /// <summary>
    /// Extended ticker including exchange prefix (e.g., "FRA:VWCE").
    /// </summary>
    public string? ExtendedTicker { get; private set; }

    /// <summary>
    /// Navigation property to parent Instrument.
    /// </summary>
    public Instrument Instrument { get; private set; } = null!;

    /// <summary>
    /// EF Core constructor.
    /// </summary>
    private EtfDetails() { }

    /// <summary>
    /// Creates a new EtfDetails record.
    /// </summary>
    public EtfDetails(
        Guid instrumentId,
        int? yearAdded = null,
        string? region = null,
        string? theme = null,
        string? manager = null,
        string? distributionType = null,
        string? domicile = null,
        string? replication = null,
        decimal? annualFeePercent = null,
        decimal? assetsMillionsEur = null,
        string? currency = null,
        string? extendedTicker = null)
    {
        Id = Guid.NewGuid();
        InstrumentId = instrumentId;
        YearAdded = yearAdded;
        Region = region;
        Theme = theme;
        Manager = manager;
        DistributionType = distributionType;
        Domicile = domicile;
        Replication = replication;
        AnnualFeePercent = annualFeePercent;
        AssetsMillionsEur = assetsMillionsEur;
        Currency = currency;
        ExtendedTicker = extendedTicker;
    }

    /// <summary>
    /// Updates the ETF details.
    /// </summary>
    public void Update(
        int? yearAdded = null,
        string? region = null,
        string? theme = null,
        string? manager = null,
        string? distributionType = null,
        string? domicile = null,
        string? replication = null,
        decimal? annualFeePercent = null,
        decimal? assetsMillionsEur = null,
        string? currency = null,
        string? extendedTicker = null)
    {
        if (yearAdded.HasValue) YearAdded = yearAdded;
        if (region != null) Region = region;
        if (theme != null) Theme = theme;
        if (manager != null) Manager = manager;
        if (distributionType != null) DistributionType = distributionType;
        if (domicile != null) Domicile = domicile;
        if (replication != null) Replication = replication;
        if (annualFeePercent.HasValue) AnnualFeePercent = annualFeePercent;
        if (assetsMillionsEur.HasValue) AssetsMillionsEur = assetsMillionsEur;
        if (currency != null) Currency = currency;
        if (extendedTicker != null) ExtendedTicker = extendedTicker;
    }
}
