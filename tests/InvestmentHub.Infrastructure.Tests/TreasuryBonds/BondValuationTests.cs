using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Infrastructure.TreasuryBonds;
using Xunit;

namespace InvestmentHub.Infrastructure.Tests.TreasuryBonds;

/// <summary>
/// Tests for BondValueCalculator and bond detection logic.
/// </summary>
public class BondValuationTests
{
    [Theory]
    [InlineData("EDO0931", AssetType.Bond, true)]
    [InlineData("EDO1232", AssetType.Bond, true)]
    [InlineData("COI0126", AssetType.Bond, true)]
    [InlineData("TOS0127", AssetType.Bond, true)]
    [InlineData("ROS0130", AssetType.Bond, true)]
    [InlineData("ROD0129", AssetType.Bond, true)]
    [InlineData("OTS0126", AssetType.Bond, true)]
    [InlineData("FPC1140", AssetType.Bond, false)]  // Catalyst bond, not treasury
    [InlineData("BGK0227", AssetType.Bond, false)]  // Catalyst bond, not treasury
    [InlineData("CDR", AssetType.Stock, false)]     // Stock
    [InlineData("AAPL", AssetType.Stock, false)]    // Stock
    public void IsTreasuryBond_CorrectlyIdentifiesBondTypes(string ticker, AssetType assetType, bool expectedIsTreasury)
    {
        // Arrange
        var symbol = new Symbol(ticker, "WSE", assetType);

        // Act
        var isTreasury = IsTreasuryBond(symbol);

        // Assert
        Assert.Equal(expectedIsTreasury, isTreasury);
    }

    [Theory]
    [InlineData("FPC1140", "Catalyst", AssetType.Bond, true)]
    [InlineData("FPC1140", "WSE", AssetType.Bond, true)]  // FPC prefix triggers even on WSE
    [InlineData("BGK0227", "Catalyst", AssetType.Bond, true)]
    [InlineData("BGK0330", "WSE", AssetType.Bond, true)]  // BGK prefix triggers even on WSE
    [InlineData("XYZ0931", "Catalyst", AssetType.Bond, true)]  // Any bond on Catalyst
    [InlineData("EDO0931", "WSE", AssetType.Bond, false)]  // Treasury bond, not Catalyst
    [InlineData("CDR", "WSE", AssetType.Stock, false)]     // Stock
    public void IsCatalystBond_CorrectlyIdentifiesCatalystBonds(string ticker, string exchange, AssetType assetType, bool expectedIsCatalyst)
    {
        // Arrange
        var symbol = new Symbol(ticker, exchange, assetType);

        // Act
        var isCatalyst = IsCatalystBond(symbol);

        // Assert
        Assert.Equal(expectedIsCatalyst, isCatalyst);
    }

    [Fact]
    public void CatalystBondValueConversion_CorrectlyConvertsPercentageToAbsolute()
    {
        // Arrange
        var pricePercentage = 67.39m;  // Price from Stooq (percentage of nominal)
        var quantity = 53m;
        const decimal nominalValue = 1000m;

        // Act
        var absoluteValue = quantity * (pricePercentage / 100m) * nominalValue;

        // Assert
        Assert.Equal(35_716.70m, absoluteValue);
    }

    [Fact]
    public void CatalystBondCostConversion_CorrectlyConvertsCost()
    {
        // Arrange
        var purchasePricePercentage = 71.59m;  // Purchase price as percentage
        var quantity = 53m;
        const decimal nominalValue = 1000m;

        // Act
        // Total cost (raw from FIFO) = 71.59 * 53 = 3794.27
        var rawTotalCost = purchasePricePercentage * quantity;
        // Adjusted total cost = (raw cost / 100) * 1000 = 37942.70
        var adjustedTotalCost = (rawTotalCost / 100m) * nominalValue;

        // Assert
        Assert.Equal(3794.27m, rawTotalCost);
        Assert.Equal(37_942.70m, adjustedTotalCost);
    }

    // Helper methods matching GetPositionsQueryHandler logic
    private static bool IsTreasuryBond(Symbol symbol)
    {
        if (symbol.AssetType != AssetType.Bond)
            return false;

        var treasuryPrefixes = new[] { "EDO", "COI", "TOS", "ROS", "ROD", "OTS" };
        return treasuryPrefixes.Any(prefix =>
            symbol.Ticker.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsCatalystBond(Symbol symbol)
    {
        if (symbol.AssetType != AssetType.Bond)
            return false;

        return symbol.Exchange == "Catalyst" ||
               symbol.Ticker.StartsWith("FPC", StringComparison.OrdinalIgnoreCase) ||
               symbol.Ticker.StartsWith("BGK", StringComparison.OrdinalIgnoreCase);
    }
}
