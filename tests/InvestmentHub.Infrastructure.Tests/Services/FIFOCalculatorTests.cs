using FluentAssertions;
using InvestmentHub.Infrastructure.Services;
using Xunit;

namespace InvestmentHub.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for FIFO (First-In, First-Out) cost basis calculation.
/// This tests the core FIFO algorithm without EF Core dependencies.
/// </summary>
public class FIFOCalculatorTests
{
    [Fact]
    public void SimpleBuySell_AllSold_ShouldReturnZeroQuantity()
    {
        // Arrange: Buy 100 @ $10, Sell 100 @ $15
        var buys = new List<(decimal quantity, decimal price, decimal fee)>
        {
            (100m, 10m, 0m)
        };
        var sells = new List<(decimal quantity, decimal price, decimal fee)>
        {
            (100m, 15m, 0m)
        };

        // Act
        var result = FIFOCalculator.CalculateSimple(buys, sells);

        // Assert
        result.RemainingQuantity.Should().Be(0m);
        result.RealizedGains.Should().Be(500m); // (15-10) * 100
    }

    [Fact]
    public void MultipleBuys_SingleSell_ShouldUseFIFO()
    {
        // Arrange: 
        // Buy 100 @ $10 (lot 1)
        // Buy 50 @ $12 (lot 2)
        // Sell 120 @ $15 (should use 100 from lot 1, 20 from lot 2)
        var buys = new List<(decimal quantity, decimal price, decimal fee)>
        {
            (100m, 10m, 0m),
            (50m, 12m, 0m)
        };
        var sells = new List<(decimal quantity, decimal price, decimal fee)>
        {
            (120m, 15m, 0m)
        };

        // Act
        var result = FIFOCalculator.CalculateSimple(buys, sells);

        // Assert
        result.RemainingQuantity.Should().Be(30m); // 150 - 120 = 30
        result.AverageCost.Should().Be(12m); // Remaining 30 all from lot 2 @ $12
        result.TotalCost.Should().Be(360m); // 30 * $12

        // Realized gains:
        // From lot 1: 100 * ($15 - $10) = $500
        // From lot 2: 20 * ($15 - $12) = $60
        // Total: $560
        result.RealizedGains.Should().Be(560m);
    }

    [Fact]
    public void MultipleBuysAndSells_ComplexScenario_ShouldCalculateCorrectly()
    {
        // Arrange:
        // Buy 100 @ $10
        // Buy 50 @ $12
        // Sell 80 @ $15
        // Buy 30 @ $13
        // Sell 60 @ $16
        var buys = new List<(decimal quantity, decimal price, decimal fee)>
        {
            (100m, 10m, 0m),
            (50m, 12m, 0m),
            (30m, 13m, 0m)
        };
        var sells = new List<(decimal quantity, decimal price, decimal fee)>
        {
            (80m, 15m, 0m),
            (60m, 16m, 0m)
        };

        // Act
        var result = FIFOCalculator.CalculateSimple(buys, sells);

        // Assert
        // Total: 180 bought, 140 sold, 40 remaining
        result.RemainingQuantity.Should().Be(40m);

        // FIFO: After first sell (80), lot1 has 20 left @ $10, lot2 is full (50 @ $12)
        // After second sell (60): consume 20 from lot1, 40 from lot2
        // Remaining: 10 from lot2 @ $12, all of lot3 (30) @ $13
        // Avg: (10*12 + 30*13) / 40 = (120+390)/40 = 12.75
        result.AverageCost.Should().BeApproximately(12.75m, 0.01m);
    }

    [Fact]
    public void FeesIncluded_ShouldAffectCostBasis()
    {
        // Arrange: Buy 100 @ $20 with $10 fee, Sell 50 @ $25 with $5 fee
        var buys = new List<(decimal quantity, decimal price, decimal fee)>
        {
            (100m, 20m, 10m) // Cost per unit = $20.10
        };
        var sells = new List<(decimal quantity, decimal price, decimal fee)>
        {
            (50m, 25m, 5m)
        };

        // Act
        var result = FIFOCalculator.CalculateSimple(buys, sells);

        // Assert
        result.RemainingQuantity.Should().Be(50m);
        result.AverageCost.Should().BeApproximately(20.10m, 0.01m);
        result.TotalCost.Should().BeApproximately(1005m, 0.01m); // 50 * $20.10

        // Realized: (50 * $25) - (50 * $20.10) - $5 fee = $1250 - $1005 - $5 = $240
        result.RealizedGains.Should().BeApproximately(240m, 0.01m);
    }

    [Fact]
    public void NoBuys_ShouldReturnZeros()
    {
        // Arrange
        var buys = new List<(decimal quantity, decimal price, decimal fee)>();
        var sells = new List<(decimal quantity, decimal price, decimal fee)>
        {
            (10m, 15m, 0m)
        };

        // Act
        var result = FIFOCalculator.CalculateSimple(buys, sells);

        // Assert
        result.RemainingQuantity.Should().Be(0m);
        result.RealizedGains.Should().Be(0m);
    }

    [Fact]
    public void NoSells_ShouldReturnFullPosition()
    {
        // Arrange
        var buys = new List<(decimal quantity, decimal price, decimal fee)>
        {
            (100m, 10m, 0m),
            (50m, 12m, 0m)
        };
        var sells = new List<(decimal quantity, decimal price, decimal fee)>();

        // Act
        var result = FIFOCalculator.CalculateSimple(buys, sells);

        // Assert
        result.RemainingQuantity.Should().Be(150m);
        // Avg: (100*10 + 50*12) / 150 = 1600/150 = 10.67
        result.AverageCost.Should().BeApproximately(10.67m, 0.01m);
        result.RealizedGains.Should().Be(0m);
    }

    [Fact]
    public void PartialSell_ShouldUpdateLot()
    {
        // Arrange: Buy 100 @ $10, Sell 40 @ $15
        var buys = new List<(decimal quantity, decimal price, decimal fee)>
        {
            (100m, 10m, 0m)
        };
        var sells = new List<(decimal quantity, decimal price, decimal fee)>
        {
            (40m, 15m, 0m)
        };

        // Act
        var result = FIFOCalculator.CalculateSimple(buys, sells);

        // Assert
        result.RemainingQuantity.Should().Be(60m);
        result.AverageCost.Should().Be(10m);
        result.TotalCost.Should().Be(600m);
        result.RealizedGains.Should().Be(200m); // 40 * (15-10)
    }

    [Fact]
    public void MultipleLots_PartialConsumption_ShouldTrackCorrectly()
    {
        // Arrange: 3 lots, sell that spans first two
        var buys = new List<(decimal quantity, decimal price, decimal fee)>
        {
            (50m, 10m, 0m),
            (50m, 12m, 0m),
            (50m, 14m, 0m)
        };
        var sells = new List<(decimal quantity, decimal price, decimal fee)>
        {
            (75m, 20m, 0m) // Takes all of lot1 (50) + 25 from lot2
        };

        // Act
        var result = FIFOCalculator.CalculateSimple(buys, sells);

        // Assert
        result.RemainingQuantity.Should().Be(75m); // 150 - 75 = 75
        
        // Remaining: 25 from lot2 @ $12, all of lot3 (50) @ $14
        // Avg: (25*12 + 50*14) / 75 = (300+700)/75 = 13.33
        result.AverageCost.Should().BeApproximately(13.33m, 0.01m);

        // Realized: 50*(20-10) + 25*(20-12) = 500 + 200 = 700
        result.RealizedGains.Should().Be(700m);
    }
}
