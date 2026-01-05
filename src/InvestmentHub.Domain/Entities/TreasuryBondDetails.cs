using InvestmentHub.Domain.Enums;

namespace InvestmentHub.Domain.Entities;

/// <summary>
/// Extended details for Polish Treasury Bonds (Obligacje Skarbowe).
/// Linked 1:1 with Instrument entity for bonds.
/// </summary>
public class TreasuryBondDetails
{
    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the linked Instrument ID.
    /// </summary>
    public Guid InstrumentId { get; private set; }

    /// <summary>
    /// Gets the type of the bond (EDO, COI, TOS, etc.).
    /// </summary>
    public BondType Type { get; private set; }

    /// <summary>
    /// Gets the issue/purchase date of the bond series.
    /// </summary>
    public DateTime IssueDate { get; private set; }

    /// <summary>
    /// Gets the maturity date when the bond will be redeemed.
    /// </summary>
    public DateTime MaturityDate { get; private set; }

    /// <summary>
    /// Gets the nominal value per bond unit (typically 100 PLN).
    /// </summary>
    public decimal NominalValue { get; private set; }

    /// <summary>
    /// Gets the first year interest rate (fixed).
    /// </summary>
    public decimal FirstYearRate { get; private set; }

    /// <summary>
    /// Gets the margin added to inflation for indexed bonds (COI, EDO, ROD).
    /// </summary>
    public decimal Margin { get; private set; }

    /// <summary>
    /// Gets the early redemption fee per bond (e.g., 0.70 PLN for EDO).
    /// </summary>
    public decimal EarlyRedemptionFee { get; private set; }

    /// <summary>
    /// Navigation property to the parent Instrument.
    /// </summary>
    public Instrument Instrument { get; private set; } = null!;

    /// <summary>
    /// Navigation property to interest periods (from PDF tables).
    /// </summary>
    public ICollection<InterestPeriod> InterestPeriods { get; private set; } = new List<InterestPeriod>();

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private TreasuryBondDetails() { }

    /// <summary>
    /// Creates a new Treasury Bond Details instance.
    /// </summary>
    public TreasuryBondDetails(
        Guid instrumentId,
        BondType type,
        DateTime issueDate,
        DateTime maturityDate,
        decimal nominalValue,
        decimal firstYearRate,
        decimal margin,
        decimal earlyRedemptionFee)
    {
        Id = Guid.NewGuid();
        InstrumentId = instrumentId;
        Type = type;
        IssueDate = issueDate;
        MaturityDate = maturityDate;
        NominalValue = nominalValue;
        FirstYearRate = firstYearRate;
        Margin = margin;
        EarlyRedemptionFee = earlyRedemptionFee;
    }

    /// <summary>
    /// Calculates the number of years until maturity.
    /// </summary>
    public int GetYearsToMaturity()
    {
        return (MaturityDate - DateTime.UtcNow).Days / 365;
    }

    /// <summary>
    /// Checks if the bond is inflation-indexed.
    /// </summary>
    public bool IsInflationIndexed()
    {
        return Type is BondType.COI or BondType.EDO or BondType.ROD or BondType.ROS;
    }
}
