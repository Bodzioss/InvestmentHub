using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Events;
using InvestmentHub.Domain.Projections;
using InvestmentHub.Domain.ReadModels;
using InvestmentHub.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace InvestmentHub.Domain.Tests.Projections;

/// <summary>
/// Unit tests for InvestmentProjection.
/// Tests event-to-read-model transformations and state updates.
/// </summary>
public class InvestmentProjectionTests
{
    private readonly InvestmentProjection _projection;

    public InvestmentProjectionTests()
    {
        _projection = new InvestmentProjection();
    }

    [Fact]
    public void Create_WithInvestmentAddedEvent_ShouldCreateReadModelWithCorrectData()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var investmentId = InvestmentId.New();
        var symbol = new Symbol("AAPL", "NASDAQ", AssetType.Stock);
        var purchasePrice = new Money(150.00m, Currency.USD);
        var quantity = 10m;
        var purchaseDate = DateTime.UtcNow.AddDays(-30);
        var initialValue = new Money(1500.00m, Currency.USD);

        var @event = new InvestmentAddedEvent(
            portfolioId,
            investmentId,
            symbol,
            purchasePrice,
            quantity,
            purchaseDate,
            initialValue);

        // Act
        var readModel = _projection.Create(@event);

        // Assert
        readModel.Should().NotBeNull();
        readModel.Id.Should().Be(investmentId.Value);
        readModel.PortfolioId.Should().Be(portfolioId.Value);
        readModel.Ticker.Should().Be("AAPL");
        readModel.Exchange.Should().Be("NASDAQ");
        readModel.AssetType.Should().Be("Stock");
        readModel.PurchasePrice.Should().Be(150.00m);
        readModel.Currency.Should().Be("USD");
        readModel.Quantity.Should().Be(10m);
        readModel.OriginalQuantity.Should().Be(10m);
        readModel.PurchaseDate.Should().Be(purchaseDate);
        readModel.CurrentValue.Should().Be(1500.00m);
        readModel.Status.Should().Be(InvestmentStatus.Active);
        readModel.SoldDate.Should().BeNull();
        readModel.RealizedProfitLoss.Should().BeNull();
    }

    [Fact]
    public void Create_WithInvestmentAddedEvent_ComputedPropertiesShouldCalculateCorrectly()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var investmentId = InvestmentId.New();
        var symbol = new Symbol("GOOGL", "NASDAQ", AssetType.Stock);
        var purchasePrice = new Money(100.00m, Currency.USD);
        var quantity = 5m;
        var initialValue = new Money(550.00m, Currency.USD); // Price went up to 110

        var @event = new InvestmentAddedEvent(
            portfolioId,
            investmentId,
            symbol,
            purchasePrice,
            quantity,
            DateTime.UtcNow.AddDays(-10),
            initialValue);

        // Act
        var readModel = _projection.Create(@event);

        // Assert
        readModel.TotalCost.Should().Be(500.00m); // 100 * 5
        readModel.UnrealizedProfitLoss.Should().Be(50.00m); // 550 - 500
        readModel.ROIPercentage.Should().BeApproximately(10.00m, 0.01m); // (50 / 500) * 100
        readModel.ValuePerUnit.Should().Be(110.00m); // 550 / 5
    }

    [Fact]
    public void Apply_WithInvestmentValueUpdatedEvent_ShouldUpdateCurrentValue()
    {
        // Arrange
        var readModel = CreateTestReadModel();
        var investmentId = new InvestmentId(readModel.Id);
        var portfolioId = new PortfolioId(readModel.PortfolioId);
        var originalValue = readModel.CurrentValue;
        var oldValue = new Money(originalValue, Currency.USD);
        var newValue = new Money(1800.00m, Currency.USD);
        var updatedAt = DateTime.UtcNow;

        var @event = new InvestmentValueUpdatedEvent(
            investmentId,
            portfolioId,
            oldValue,
            newValue,
            updatedAt);

        // Act
        InvestmentProjection.Apply(readModel, @event);

        // Assert
        readModel.CurrentValue.Should().Be(1800.00m);
        readModel.CurrentValue.Should().NotBe(originalValue);
        readModel.LastUpdated.Should().Be(updatedAt);
    }

    [Fact]
    public void Apply_WithPartialSaleEvent_ShouldUpdateQuantityAndStatus()
    {
        // Arrange
        var readModel = CreateTestReadModel();
        readModel.Quantity = 10m;
        readModel.CurrentValue = 1500.00m;

        var investmentId = new InvestmentId(readModel.Id);
        var portfolioId = new PortfolioId(readModel.PortfolioId);
        var quantitySold = 4m;
        var salePrice = new Money(160.00m, Currency.USD);
        var purchasePrice = new Money(150.00m, Currency.USD);
        var saleDate = DateTime.UtcNow;

        var @event = new InvestmentSoldEvent(
            investmentId,
            portfolioId,
            salePrice,
            quantitySold,
            saleDate,
            purchasePrice,
            isCompleteSale: false);

        // Act
        InvestmentProjection.Apply(readModel, @event);

        // Assert
        readModel.Quantity.Should().Be(6m); // 10 - 4
        readModel.Status.Should().Be(InvestmentStatus.PartiallySold);
        readModel.SoldDate.Should().Be(saleDate);
        readModel.RealizedProfitLoss.Should().Be(40.00m); // (160 - 150) * 4
        readModel.CurrentValue.Should().Be(900.00m); // Recalculated for remaining 6 units
    }

    [Fact]
    public void Apply_WithCompleteSaleEvent_ShouldSetStatusToSold()
    {
        // Arrange
        var readModel = CreateTestReadModel();
        readModel.Quantity = 10m;
        readModel.CurrentValue = 1500.00m;

        var investmentId = new InvestmentId(readModel.Id);
        var portfolioId = new PortfolioId(readModel.PortfolioId);
        var quantitySold = 10m;
        var salePrice = new Money(160.00m, Currency.USD);
        var purchasePrice = new Money(150.00m, Currency.USD);
        var saleDate = DateTime.UtcNow;

        var @event = new InvestmentSoldEvent(
            investmentId,
            portfolioId,
            salePrice,
            quantitySold,
            saleDate,
            purchasePrice,
            isCompleteSale: true);

        // Act
        InvestmentProjection.Apply(readModel, @event);

        // Assert
        readModel.Quantity.Should().Be(0m);
        readModel.Status.Should().Be(InvestmentStatus.Sold);
        readModel.CurrentValue.Should().Be(0m);
        readModel.RealizedProfitLoss.Should().Be(100.00m); // (160 - 150) * 10
        readModel.SoldDate.Should().Be(saleDate);
    }

    [Fact]
    public void Apply_WithInvestmentDeletedEvent_ShouldReturnNull()
    {
        // Arrange
        var readModel = CreateTestReadModel();
        var investmentId = new InvestmentId(readModel.Id);
        var portfolioId = new PortfolioId(readModel.PortfolioId);

        var @event = new InvestmentDeletedEvent(
            investmentId,
            portfolioId,
            "User requested deletion",
            DateTime.UtcNow);

        // Act
        var result = InvestmentProjection.Apply(readModel, @event);

        // Assert
        result.Should().BeNull(); // Signals Marten to delete the document
    }

    [Fact]
    public void MultipleEvents_InSequence_ShouldProduceCorrectFinalState()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var investmentId = InvestmentId.New();
        var symbol = new Symbol("TSLA", "NASDAQ", AssetType.Stock);
        var purchasePrice = new Money(200.00m, Currency.USD);
        var quantity = 10m;

        // Event 1: Investment Added
        var addedEvent = new InvestmentAddedEvent(
            portfolioId,
            investmentId,
            symbol,
            purchasePrice,
            quantity,
            DateTime.UtcNow.AddDays(-60),
            new Money(2000.00m, Currency.USD));

        var readModel = _projection.Create(addedEvent);

        // Event 2: Value Update (price goes up)
        var valueUpdateEvent1 = new InvestmentValueUpdatedEvent(
            investmentId,
            portfolioId,
            new Money(2000.00m, Currency.USD),
            new Money(2500.00m, Currency.USD), // $250 per share
            DateTime.UtcNow.AddDays(-30));

        InvestmentProjection.Apply(readModel, valueUpdateEvent1);

        // Event 3: Partial Sale
        var partialSaleEvent = new InvestmentSoldEvent(
            investmentId,
            portfolioId,
            new Money(250.00m, Currency.USD),
            5m, // Sell half
            DateTime.UtcNow.AddDays(-15),
            purchasePrice,
            isCompleteSale: false);

        InvestmentProjection.Apply(readModel, partialSaleEvent);

        // Event 4: Another Value Update
        var valueUpdateEvent2 = new InvestmentValueUpdatedEvent(
            investmentId,
            portfolioId,
            new Money(1250.00m, Currency.USD), // Previous value of 5 units
            new Money(1500.00m, Currency.USD), // Remaining 5 units at $300
            DateTime.UtcNow);

        InvestmentProjection.Apply(readModel, valueUpdateEvent2);

        // Assert final state
        readModel.Quantity.Should().Be(5m);
        readModel.OriginalQuantity.Should().Be(10m);
        readModel.Status.Should().Be(InvestmentStatus.PartiallySold);
        readModel.CurrentValue.Should().Be(1500.00m);
        readModel.RealizedProfitLoss.Should().Be(250.00m); // (250 - 200) * 5
        readModel.TotalCost.Should().Be(1000.00m); // 200 * 5 remaining
        readModel.UnrealizedProfitLoss.Should().Be(500.00m); // 1500 - 1000
        readModel.ROIPercentage.Should().BeApproximately(50.00m, 0.01m);
    }

    [Fact]
    public void ComputedProperties_WithZeroQuantity_ShouldReturnZero()
    {
        // Arrange
        var readModel = CreateTestReadModel();
        readModel.Quantity = 0m;
        readModel.CurrentValue = 0m;
        readModel.Status = InvestmentStatus.Sold;

        // Assert
        readModel.TotalCost.Should().Be(0m);
        readModel.UnrealizedProfitLoss.Should().Be(0m); // Sold status returns 0
        readModel.ValuePerUnit.Should().Be(0m);
    }

    [Fact]
    public void ROIPercentage_ForSoldInvestment_ShouldCalculateFromRealizedProfitLoss()
    {
        // Arrange
        var readModel = CreateTestReadModel();
        readModel.Status = InvestmentStatus.Sold;
        readModel.PurchasePrice = 100m;
        readModel.OriginalQuantity = 10m;
        readModel.Quantity = 0m;
        readModel.RealizedProfitLoss = 200m; // Made $200 profit
        readModel.CurrentValue = 0m;

        // Act & Assert
        // Original cost: 100 * 10 = 1000
        // ROI: (200 / 1000) * 100 = 20%
        readModel.ROIPercentage.Should().BeApproximately(20.00m, 0.01m);
    }

    /// <summary>
    /// Helper method to create a test read model with default values.
    /// </summary>
    private static InvestmentReadModel CreateTestReadModel()
    {
        return new InvestmentReadModel
        {
            Id = Guid.NewGuid(),
            PortfolioId = Guid.NewGuid(),
            Ticker = "TEST",
            Exchange = "NASDAQ",
            AssetType = "Stock",
            PurchasePrice = 150.00m,
            Currency = "USD",
            Quantity = 10m,
            OriginalQuantity = 10m,
            PurchaseDate = DateTime.UtcNow.AddDays(-30),
            CurrentValue = 1500.00m,
            Status = InvestmentStatus.Active,
            LastUpdated = DateTime.UtcNow,
            SoldDate = null,
            RealizedProfitLoss = null
        };
    }
}
