using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Enums;

namespace InvestmentHub.Infrastructure.TreasuryBonds;

/// <summary>
/// Calculates current value and returns for Polish Treasury Bonds.
/// </summary>
public class BondValueCalculator
{
    /// <summary>
    /// Calculates the current value of bonds at a specific date.
    /// </summary>
    /// <param name="bondDetails">The bond details including interest periods</param>
    /// <param name="quantity">Number of bonds held</param>
    /// <param name="asOfDate">Date to calculate value for (default: today)</param>
    /// <returns>Calculated bond value details</returns>
    public BondValueResult Calculate(TreasuryBondDetails bondDetails, int quantity, DateTime? asOfDate = null)
    {
        var date = asOfDate ?? DateTime.UtcNow;

        // Find the current interest period
        var currentPeriod = bondDetails.InterestPeriods
            .FirstOrDefault(p => date >= p.StartDate && date <= p.EndDate);

        // Calculate completed periods interest
        var completedPeriodsInterest = bondDetails.InterestPeriods
            .Where(p => p.EndDate < date)
            .Sum(p => p.AccruedInterest);

        // Calculate current period accrued interest
        var currentPeriodInterest = currentPeriod?.GetAccruedInterestAt(date) ?? 0;

        var totalInterestPerBond = completedPeriodsInterest + currentPeriodInterest;
        var nominalValue = bondDetails.NominalValue;
        var grossValuePerBond = nominalValue + totalInterestPerBond;

        // Apply tax (19% Belka tax on interest)
        var taxPerBond = totalInterestPerBond * 0.19m;
        var netValuePerBond = nominalValue + totalInterestPerBond - taxPerBond;

        return new BondValueResult
        {
            Quantity = quantity,
            NominalValuePerBond = nominalValue,
            TotalNominalValue = nominalValue * quantity,
            AccruedInterestPerBond = totalInterestPerBond,
            TotalAccruedInterest = totalInterestPerBond * quantity,
            GrossValuePerBond = grossValuePerBond,
            TotalGrossValue = grossValuePerBond * quantity,
            TaxPerBond = taxPerBond,
            TotalTax = taxPerBond * quantity,
            NetValuePerBond = netValuePerBond,
            TotalNetValue = netValuePerBond * quantity,
            CurrentPeriodNumber = currentPeriod?.PeriodNumber,
            CurrentInterestRate = currentPeriod?.InterestRate,
            DaysInCurrentPeriod = currentPeriod != null ? (date - currentPeriod.StartDate).Days : 0,
            MaturityDate = bondDetails.MaturityDate,
            DaysToMaturity = (bondDetails.MaturityDate - date).Days
        };
    }

    /// <summary>
    /// Calculates value if bonds are redeemed early (before maturity).
    /// </summary>
    public BondValueResult CalculateEarlyRedemption(TreasuryBondDetails bondDetails, int quantity, DateTime? asOfDate = null)
    {
        var result = Calculate(bondDetails, quantity, asOfDate);

        // Apply early redemption fee
        var feePerBond = bondDetails.EarlyRedemptionFee;
        var totalFee = feePerBond * quantity;

        result.EarlyRedemptionFeePerBond = feePerBond;
        result.TotalEarlyRedemptionFee = totalFee;
        result.NetValueAfterEarlyRedemption = result.TotalNetValue - totalFee;
        result.IsEarlyRedemption = true;

        return result;
    }

    /// <summary>
    /// Simulates future value at maturity.
    /// Assumes average inflation for remaining indexed periods.
    /// </summary>
    public BondValueResult SimulateAtMaturity(
        TreasuryBondDetails bondDetails,
        int quantity,
        decimal assumedInflation = 2.5m)
    {
        var maturityDate = bondDetails.MaturityDate;

        // For simulation, we need to project future interest rates
        // For indexed bonds: rate = margin + inflation
        var projectedTotalInterest = 0m;
        var currentDate = DateTime.UtcNow;

        foreach (var period in bondDetails.InterestPeriods.OrderBy(p => p.PeriodNumber))
        {
            if (period.EndDate <= currentDate)
            {
                // Completed period - use actual interest
                projectedTotalInterest += period.AccruedInterest;
            }
            else if (period.InterestRate > 0)
            {
                // Known future rate
                projectedTotalInterest += period.AccruedInterest;
            }
            else if (bondDetails.IsInflationIndexed())
            {
                // Project based on assumed inflation
                var projectedRate = bondDetails.Margin + assumedInflation;
                var periodDays = (period.EndDate - period.StartDate).Days;
                var projectedInterest = bondDetails.NominalValue * projectedRate / 100 * periodDays / 365;
                projectedTotalInterest += projectedInterest;
            }
        }

        var grossValue = bondDetails.NominalValue + projectedTotalInterest;
        var tax = projectedTotalInterest * 0.19m;
        var netValue = grossValue - tax;

        return new BondValueResult
        {
            Quantity = quantity,
            NominalValuePerBond = bondDetails.NominalValue,
            TotalNominalValue = bondDetails.NominalValue * quantity,
            AccruedInterestPerBond = projectedTotalInterest,
            TotalAccruedInterest = projectedTotalInterest * quantity,
            GrossValuePerBond = grossValue,
            TotalGrossValue = grossValue * quantity,
            TaxPerBond = tax,
            TotalTax = tax * quantity,
            NetValuePerBond = netValue,
            TotalNetValue = netValue * quantity,
            MaturityDate = maturityDate,
            DaysToMaturity = (maturityDate - DateTime.UtcNow).Days,
            IsProjection = true,
            AssumedInflationRate = assumedInflation
        };
    }
}

/// <summary>
/// Result of bond value calculation.
/// </summary>
public class BondValueResult
{
    public int Quantity { get; set; }
    public decimal NominalValuePerBond { get; set; }
    public decimal TotalNominalValue { get; set; }
    public decimal AccruedInterestPerBond { get; set; }
    public decimal TotalAccruedInterest { get; set; }
    public decimal GrossValuePerBond { get; set; }
    public decimal TotalGrossValue { get; set; }
    public decimal TaxPerBond { get; set; }
    public decimal TotalTax { get; set; }
    public decimal NetValuePerBond { get; set; }
    public decimal TotalNetValue { get; set; }

    // Current period info
    public int? CurrentPeriodNumber { get; set; }
    public decimal? CurrentInterestRate { get; set; }
    public int DaysInCurrentPeriod { get; set; }

    // Maturity info
    public DateTime MaturityDate { get; set; }
    public int DaysToMaturity { get; set; }

    // Early redemption
    public bool IsEarlyRedemption { get; set; }
    public decimal EarlyRedemptionFeePerBond { get; set; }
    public decimal TotalEarlyRedemptionFee { get; set; }
    public decimal NetValueAfterEarlyRedemption { get; set; }

    // Projection
    public bool IsProjection { get; set; }
    public decimal AssumedInflationRate { get; set; }
}
