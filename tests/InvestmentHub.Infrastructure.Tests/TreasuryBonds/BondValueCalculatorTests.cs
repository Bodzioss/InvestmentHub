using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Infrastructure.TreasuryBonds;
using Xunit;

namespace InvestmentHub.Infrastructure.Tests.TreasuryBonds;

/// <summary>
/// Tests for BondValueCalculator.
/// </summary>
public class BondValueCalculatorTests
{
    private readonly BondValueCalculator _calculator = new();

    [Fact]
    public void Calculate_WithNoInterestPeriods_ReturnsNominalValue()
    {
        // Arrange
        var instrumentId = Guid.NewGuid();
        var bondDetails = new TreasuryBondDetails(
            instrumentId,
            BondType.EDO,
            issueDate: new DateTime(2021, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            maturityDate: new DateTime(2031, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            nominalValue: 100m,
            firstYearRate: 6.80m,
            margin: 1.75m,
            earlyRedemptionFee: 2.00m
        );
        var quantity = 10;

        // Act
        var result = _calculator.Calculate(bondDetails, quantity);

        // Assert
        Assert.Equal(10, result.Quantity);
        Assert.Equal(100m, result.NominalValuePerBond);
        Assert.Equal(1000m, result.TotalNominalValue); // 10 * 100
        Assert.Equal(0m, result.AccruedInterestPerBond); // No periods = no interest
        Assert.Equal(100m, result.NetValuePerBond); // Just nominal, no interest
        Assert.Equal(1000m, result.TotalNetValue); // 10 * 100
    }

    [Fact]
    public void Calculate_WithCompletedInterestPeriod_IncludesAccruedInterest()
    {
        // Arrange
        var instrumentId = Guid.NewGuid();
        var bondDetails = new TreasuryBondDetails(
            instrumentId,
            BondType.EDO,
            issueDate: new DateTime(2023, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            maturityDate: new DateTime(2033, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            nominalValue: 100m,
            firstYearRate: 6.80m,
            margin: 1.75m,
            earlyRedemptionFee: 2.00m
        );

        // Add a completed interest period (first year)
        var period1 = new InterestPeriod(
            bondDetails.Id,
            periodNumber: 1,
            startDate: new DateTime(2023, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2024, 8, 31, 0, 0, 0, DateTimeKind.Utc),
            interestRate: 6.80m,
            accruedInterest: 6.80m  // Full year interest
        );
        bondDetails.InterestPeriods.Add(period1);

        var quantity = 1;
        var asOfDate = new DateTime(2024, 10, 1, 0, 0, 0, DateTimeKind.Utc); // After first period ends

        // Act
        var result = _calculator.Calculate(bondDetails, quantity, asOfDate);

        // Assert
        Assert.Equal(1, result.Quantity);
        Assert.Equal(6.80m, result.AccruedInterestPerBond);
        Assert.Equal(106.80m, result.GrossValuePerBond); // 100 + 6.80

        // Tax = 6.80 * 0.19 = 1.292
        Assert.Equal(1.292m, result.TaxPerBond);

        // Net = 100 + 6.80 - 1.292 = 105.508
        Assert.Equal(105.508m, result.NetValuePerBond);
    }

    [Fact]
    public void CalculateEarlyRedemption_SubtractsEarlyRedemptionFee()
    {
        // Arrange
        var instrumentId = Guid.NewGuid();
        var bondDetails = new TreasuryBondDetails(
            instrumentId,
            BondType.EDO,
            issueDate: new DateTime(2023, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            maturityDate: new DateTime(2033, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            nominalValue: 100m,
            firstYearRate: 6.80m,
            margin: 1.75m,
            earlyRedemptionFee: 2.00m  // EDO has 2 PLN fee
        );

        var quantity = 5;

        // Act
        var result = _calculator.CalculateEarlyRedemption(bondDetails, quantity);

        // Assert
        Assert.True(result.IsEarlyRedemption);
        Assert.Equal(2.00m, result.EarlyRedemptionFeePerBond);
        Assert.Equal(10.00m, result.TotalEarlyRedemptionFee); // 5 * 2 PLN
        Assert.Equal(result.TotalNetValue - 10.00m, result.NetValueAfterEarlyRedemption);
    }

    [Fact]
    public void Calculate_ReturnsCorrectMaturityInfo()
    {
        // Arrange
        var instrumentId = Guid.NewGuid();
        var maturityDate = new DateTime(2031, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var bondDetails = new TreasuryBondDetails(
            instrumentId,
            BondType.EDO,
            issueDate: new DateTime(2021, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            maturityDate: maturityDate,
            nominalValue: 100m,
            firstYearRate: 6.80m,
            margin: 1.75m,
            earlyRedemptionFee: 2.00m
        );

        var asOfDate = new DateTime(2026, 1, 6, 0, 0, 0, DateTimeKind.Utc);
        var expectedDaysToMaturity = (maturityDate - asOfDate).Days;

        // Act
        var result = _calculator.Calculate(bondDetails, 1, asOfDate);

        // Assert
        Assert.Equal(maturityDate, result.MaturityDate);
        Assert.Equal(expectedDaysToMaturity, result.DaysToMaturity);
    }
}
