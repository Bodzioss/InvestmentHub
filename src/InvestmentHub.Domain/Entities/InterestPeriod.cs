namespace InvestmentHub.Domain.Entities;

/// <summary>
/// Represents a single interest period for a Treasury Bond.
/// Data is typically sourced from PDF interest tables from obligacjeskarbowe.pl.
/// </summary>
public class InterestPeriod
{
    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the parent Treasury Bond Details ID.
    /// </summary>
    public Guid BondDetailsId { get; private set; }

    /// <summary>
    /// Gets the period number (1, 2, 3, etc.).
    /// </summary>
    public int PeriodNumber { get; private set; }

    /// <summary>
    /// Gets the start date of this interest period.
    /// </summary>
    public DateTime StartDate { get; private set; }

    /// <summary>
    /// Gets the end date of this interest period.
    /// </summary>
    public DateTime EndDate { get; private set; }

    /// <summary>
    /// Gets the annual interest rate for this period (as percentage, e.g., 5.75).
    /// </summary>
    public decimal InterestRate { get; private set; }

    /// <summary>
    /// Gets the total accrued interest per bond at end of period (in PLN).
    /// </summary>
    public decimal AccruedInterest { get; private set; }

    /// <summary>
    /// Navigation property to parent.
    /// </summary>
    public TreasuryBondDetails BondDetails { get; private set; } = null!;

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private InterestPeriod() { }

    /// <summary>
    /// Creates a new Interest Period.
    /// </summary>
    public InterestPeriod(
        Guid bondDetailsId,
        int periodNumber,
        DateTime startDate,
        DateTime endDate,
        decimal interestRate,
        decimal accruedInterest)
    {
        Id = Guid.NewGuid();
        BondDetailsId = bondDetailsId;
        PeriodNumber = periodNumber;
        StartDate = startDate;
        EndDate = endDate;
        InterestRate = interestRate;
        AccruedInterest = accruedInterest;
    }

    /// <summary>
    /// Calculates daily interest for this period.
    /// </summary>
    public decimal GetDailyInterest()
    {
        var days = (EndDate - StartDate).Days;
        return days > 0 ? AccruedInterest / days : 0;
    }

    /// <summary>
    /// Calculates accrued interest up to a specific date within this period.
    /// </summary>
    public decimal GetAccruedInterestAt(DateTime date)
    {
        if (date <= StartDate) return 0;
        if (date >= EndDate) return AccruedInterest;

        var daysElapsed = (date - StartDate).Days;
        return GetDailyInterest() * daysElapsed;
    }
}
