using FluentAssertions;
using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Events;
using InvestmentHub.Domain.ValueObjects;
using Xunit;

namespace InvestmentHub.Domain.Tests.Aggregates;

public class InvestmentAggregateTests
{
    private readonly InvestmentId _investmentId = InvestmentId.New();
    private readonly PortfolioId _portfolioId = PortfolioId.New();
    private readonly Symbol _symbol = new("AAPL", "NASDAQ", AssetType.Stock);
    private readonly Money _purchasePrice = new(100m, Currency.USD);
    private readonly decimal _quantity = 10m;
    private readonly DateTime _purchaseDate = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    #region Create Tests

    [Fact]
    public void Create_ShouldGenerateInvestmentAddedEvent_WithCorrectData()
    {
        // Act
        var aggregate = InvestmentAggregate.Create(
            _investmentId,
            _portfolioId,
            _symbol,
            _purchasePrice,
            _quantity,
            _purchaseDate);

        // Assert
        aggregate.Should().NotBeNull();
        aggregate.Id.Should().Be(_investmentId.Value);
        aggregate.PortfolioId.Should().Be(_portfolioId);
        aggregate.Symbol.Should().Be(_symbol);
        aggregate.PurchasePrice.Should().Be(_purchasePrice);
        aggregate.Quantity.Should().Be(_quantity);
        aggregate.OriginalQuantity.Should().Be(_quantity);
        aggregate.PurchaseDate.Should().Be(_purchaseDate);
        aggregate.Status.Should().Be(InvestmentStatus.Active);
        
        // Initial current value should equal purchase price * quantity
        aggregate.CurrentValue.Amount.Should().Be(_purchasePrice.Amount * _quantity);
        aggregate.CurrentValue.Currency.Should().Be(_purchasePrice.Currency);

        // Check generated event
        var events = aggregate.GetUncommittedEvents().ToList();
        events.Should().HaveCount(1);
        
        var addedEvent = events[0] as InvestmentAddedEvent;
        addedEvent.Should().NotBeNull();
        addedEvent!.InvestmentId.Should().Be(_investmentId);
        addedEvent.PortfolioId.Should().Be(_portfolioId);
        addedEvent.Symbol.Should().Be(_symbol);
        addedEvent.PurchasePrice.Should().Be(_purchasePrice);
        addedEvent.Quantity.Should().Be(_quantity);
        addedEvent.PurchaseDate.Should().Be(_purchaseDate);
        addedEvent.InitialCurrentValue.Amount.Should().Be(_purchasePrice.Amount * _quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_ShouldThrowException_WhenQuantityIsInvalid(decimal quantity)
    {
        // Act & Assert
        var act = () => InvestmentAggregate.Create(
            _investmentId,
            _portfolioId,
            _symbol,
            _purchasePrice,
            quantity,
            _purchaseDate);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*greater than zero*")
            .And.ParamName.Should().Be("quantity");
    }

    [Fact]
    public void Create_ShouldThrowException_WhenPurchasePriceIsZero()
    {
        // Act & Assert
        var act = () => InvestmentAggregate.Create(
            _investmentId,
            _portfolioId,
            _symbol,
            new Money(0m, Currency.USD),
            _quantity,
            _purchaseDate);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*greater than zero*")
            .And.ParamName.Should().Be("purchasePrice");
    }



    [Fact]
    public void Create_ShouldThrowException_WhenPurchaseDateIsInFuture()
    {
        // Act & Assert
        var futureDate = DateTime.UtcNow.AddDays(1);
        
        var act = () => InvestmentAggregate.Create(
            _investmentId,
            _portfolioId,
            _symbol,
            _purchasePrice,
            _quantity,
            futureDate);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*future*")
            .And.ParamName.Should().Be("purchaseDate");
    }

    #endregion

    #region UpdateValue Tests

    [Fact]
    public void UpdateValue_ShouldGenerateInvestmentValueUpdatedEvent()
    {
        // Arrange
        var aggregate = InvestmentAggregate.Create(
            _investmentId,
            _portfolioId,
            _symbol,
            _purchasePrice,
            _quantity,
            _purchaseDate);

        var oldValue = aggregate.CurrentValue;
        var newValuePerUnit = new Money(150m, Currency.USD);

        // Act
        aggregate.UpdateValue(newValuePerUnit);

        // Assert
        aggregate.CurrentValue.Amount.Should().Be(newValuePerUnit.Amount * _quantity); // 150 * 10 = 1500
        
        var events = aggregate.GetUncommittedEvents().Skip(1).ToList(); // Skip InvestmentAddedEvent
        events.Should().HaveCount(1);
        
        var updatedEvent = events[0] as InvestmentValueUpdatedEvent;
        updatedEvent.Should().NotBeNull();
        updatedEvent!.InvestmentId.Should().Be(_investmentId);
        updatedEvent.OldValue.Should().Be(oldValue);
        updatedEvent.NewValue.Amount.Should().Be(newValuePerUnit.Amount * _quantity);
    }

    [Fact]
    public void UpdateValue_ShouldNotGenerateEvent_WhenValueUnchanged()
    {
        // Arrange
        var aggregate = InvestmentAggregate.Create(
            _investmentId,
            _portfolioId,
            _symbol,
            _purchasePrice,
            _quantity,
            _purchaseDate);

        var currentValuePerUnit = _purchasePrice; // Same as purchase price

        // Act
        aggregate.UpdateValue(currentValuePerUnit);

        // Assert - Only InvestmentAddedEvent should exist (no ValueUpdated event)
        var events = aggregate.GetUncommittedEvents().ToList();
        events.Should().HaveCount(1);
        events[0].Should().BeOfType<InvestmentAddedEvent>();
    }

    [Fact]
    public void UpdateValue_ShouldThrowException_WhenInvestmentIsSold()
    {
        // Arrange
        var aggregate = InvestmentAggregate.Create(
            _investmentId,
            _portfolioId,
            _symbol,
            _purchasePrice,
            _quantity,
            _purchaseDate);

        // Sell the investment completely
        aggregate.Sell(new Money(120m, Currency.USD), null, DateTime.UtcNow);

        // Act & Assert
        var act = () => aggregate.UpdateValue(new Money(150m, Currency.USD));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*sold*");
    }



    [Fact]
    public void UpdateValue_ShouldThrowException_WhenCurrencyMismatch()
    {
        // Arrange
        var aggregate = InvestmentAggregate.Create(
            _investmentId,
            _portfolioId,
            _symbol,
            _purchasePrice,
            _quantity,
            _purchaseDate);

        // Act & Assert
        var act = () => aggregate.UpdateValue(new Money(150m, Currency.EUR));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*mismatch*")
            .And.ParamName.Should().Be("newValuePerUnit");
    }

    #endregion

    #region Sell Tests

    [Fact]
    public void Sell_ShouldGenerateInvestmentSoldEvent_ForCompleteSale()
    {
        // Arrange
        var aggregate = InvestmentAggregate.Create(
            _investmentId,
            _portfolioId,
            _symbol,
            _purchasePrice,
            _quantity,
            _purchaseDate);

        var salePricePerUnit = new Money(120m, Currency.USD);
        var saleDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act - Sell all (null quantity means sell all)
        aggregate.Sell(salePricePerUnit, null, saleDate);

        // Assert
        aggregate.Status.Should().Be(InvestmentStatus.Sold);
        aggregate.Quantity.Should().Be(0);
        aggregate.CurrentValue.Amount.Should().Be(0);
        aggregate.SoldDate.Should().Be(saleDate);
        aggregate.RealizedProfitLoss.Should().NotBeNull();
        
        // Profit = (120 - 100) * 10 = 200
        aggregate.RealizedProfitLoss!.Amount.Should().Be(200m);

        var events = aggregate.GetUncommittedEvents().Skip(1).ToList(); // Skip InvestmentAddedEvent
        events.Should().HaveCount(1);
        
        var soldEvent = events[0] as InvestmentSoldEvent;
        soldEvent.Should().NotBeNull();
        soldEvent!.InvestmentId.Should().Be(_investmentId);
        soldEvent.SalePrice.Should().Be(salePricePerUnit);
        soldEvent.QuantitySold.Should().Be(_quantity);
        soldEvent.IsCompleteSale.Should().BeTrue();
        soldEvent.RealizedProfitLoss.Amount.Should().Be(200m);
        soldEvent.ROIPercentage.Should().Be(20m); // (200 / 1000) * 100 = 20%
    }

    [Fact]
    public void Sell_ShouldGenerateInvestmentSoldEvent_ForPartialSale()
    {
        // Arrange
        var aggregate = InvestmentAggregate.Create(
            _investmentId,
            _portfolioId,
            _symbol,
            _purchasePrice,
            _quantity,
            _purchaseDate);

        var salePricePerUnit = new Money(110m, Currency.USD);
        var quantityToSell = 4m;
        var saleDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act - Sell partial
        aggregate.Sell(salePricePerUnit, quantityToSell, saleDate);

        // Assert
        aggregate.Status.Should().Be(InvestmentStatus.PartiallySold);
        aggregate.Quantity.Should().Be(6m); // 10 - 4
        aggregate.OriginalQuantity.Should().Be(10m);
        
        // Current value should remain at purchase price per unit * remaining quantity (100 * 6 = 600)
        // because we haven't called UpdateValue after the sale
        aggregate.CurrentValue.Amount.Should().Be(600m); // 100 * 6 = 600
        aggregate.SoldDate.Should().Be(saleDate);
        
        // Profit = (110 - 100) * 4 = 40
        aggregate.RealizedProfitLoss!.Amount.Should().Be(40m);

        var events = aggregate.GetUncommittedEvents().Skip(1).ToList();
        events.Should().HaveCount(1);
        
        var soldEvent = events[0] as InvestmentSoldEvent;
        soldEvent.Should().NotBeNull();
        soldEvent!.QuantitySold.Should().Be(quantityToSell);
        soldEvent.IsCompleteSale.Should().BeFalse();
        soldEvent.RealizedProfitLoss.Amount.Should().Be(40m);
        soldEvent.ROIPercentage.Should().Be(10m); // (40 / 400) * 100 = 10%
    }

    [Fact]
    public void Sell_ShouldThrowException_WhenAlreadyFullySold()
    {
        // Arrange
        var aggregate = InvestmentAggregate.Create(
            _investmentId,
            _portfolioId,
            _symbol,
            _purchasePrice,
            _quantity,
            _purchaseDate);

        // First sale - complete
        aggregate.Sell(new Money(120m, Currency.USD), null, DateTime.UtcNow);

        // Act & Assert - Try to sell again
        var act = () => aggregate.Sell(new Money(130m, Currency.USD), null, DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already*sold*");
    }



    [Fact]
    public void Sell_ShouldThrowException_WhenCurrencyMismatch()
    {
        // Arrange
        var aggregate = InvestmentAggregate.Create(
            _investmentId,
            _portfolioId,
            _symbol,
            _purchasePrice,
            _quantity,
            _purchaseDate);

        // Act & Assert
        var act = () => aggregate.Sell(new Money(120m, Currency.EUR), null, DateTime.UtcNow);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*mismatch*")
            .And.ParamName.Should().Be("salePrice");
    }

    [Fact]
    public void Sell_ShouldThrowException_WhenSaleDateIsInFuture()
    {
        // Arrange
        var aggregate = InvestmentAggregate.Create(
            _investmentId,
            _portfolioId,
            _symbol,
            _purchasePrice,
            _quantity,
            _purchaseDate);

        var futureDate = DateTime.UtcNow.AddDays(1);

        // Act & Assert
        var act = () => aggregate.Sell(new Money(120m, Currency.USD), null, futureDate);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*future*")
            .And.ParamName.Should().Be("saleDate");
    }

    [Fact]
    public void Sell_ShouldThrowException_WhenQuantityToSellIsZero()
    {
        // Arrange
        var aggregate = InvestmentAggregate.Create(
            _investmentId,
            _portfolioId,
            _symbol,
            _purchasePrice,
            _quantity,
            _purchaseDate);

        // Act & Assert
        var act = () => aggregate.Sell(new Money(120m, Currency.USD), 0m, DateTime.UtcNow);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*greater than zero*")
            .And.ParamName.Should().Be("quantityToSell");
    }

    [Fact]
    public void Sell_ShouldThrowException_WhenQuantityToSellExceedsAvailable()
    {
        // Arrange
        var aggregate = InvestmentAggregate.Create(
            _investmentId,
            _portfolioId,
            _symbol,
            _purchasePrice,
            _quantity,
            _purchaseDate);

        // Act & Assert
        var act = () => aggregate.Sell(new Money(120m, Currency.USD), 15m, DateTime.UtcNow);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Cannot sell 15 units*")
            .And.ParamName.Should().Be("quantityToSell");
    }

    [Fact]
    public void Sell_ShouldAllowMultiplePartialSales()
    {
        // Arrange
        var aggregate = InvestmentAggregate.Create(
            _investmentId,
            _portfolioId,
            _symbol,
            _purchasePrice,
            _quantity, // 10 units
            _purchaseDate);

        // Act - First partial sale
        aggregate.Sell(new Money(110m, Currency.USD), 3m, DateTime.UtcNow);
        aggregate.Quantity.Should().Be(7m);
        aggregate.Status.Should().Be(InvestmentStatus.PartiallySold);

        // Second partial sale
        aggregate.Sell(new Money(115m, Currency.USD), 3m, DateTime.UtcNow);
        aggregate.Quantity.Should().Be(4m);
        aggregate.Status.Should().Be(InvestmentStatus.PartiallySold);

        // Final sale
        aggregate.Sell(new Money(120m, Currency.USD), 4m, DateTime.UtcNow);
        aggregate.Quantity.Should().Be(0m);
        aggregate.Status.Should().Be(InvestmentStatus.Sold);

        // Should have 4 events total: 1 Added + 3 Sold
        var events = aggregate.GetUncommittedEvents().ToList();
        events.Should().HaveCount(4);
        events[0].Should().BeOfType<InvestmentAddedEvent>();
        events[1].Should().BeOfType<InvestmentSoldEvent>();
        events[2].Should().BeOfType<InvestmentSoldEvent>();
        events[3].Should().BeOfType<InvestmentSoldEvent>();
    }

    #endregion

    #region Event Sourcing Tests

    [Fact]
    public void EventSourcing_ShouldReplayEventsToRebuildState()
    {
        // Arrange - Create a sequence of events manually
        var investmentId = InvestmentId.New();
        var portfolioId = PortfolioId.New();
        var symbol = new Symbol("TSLA", "NASDAQ", AssetType.Stock);
        var purchasePrice = new Money(200m, Currency.USD);
        var quantity = 5m;
        var purchaseDate = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        var addedEvent = new InvestmentAddedEvent(
            portfolioId,
            investmentId,
            symbol,
            purchasePrice,
            quantity,
            purchaseDate,
            new Money(purchasePrice.Amount * quantity, Currency.USD));

        var valueUpdatedEvent = new InvestmentValueUpdatedEvent(
            investmentId,
            portfolioId,
            new Money(1000m, Currency.USD), // Old value
            new Money(1250m, Currency.USD), // New value (250 per unit * 5)
            new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc));

        var soldEvent = new InvestmentSoldEvent(
            investmentId,
            portfolioId,
            new Money(270m, Currency.USD), // Sale price per unit
            quantity,
            new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            purchasePrice,
            true); // Complete sale

        // Act - Replay events to rebuild state
        var aggregate = new InvestmentAggregate();
        aggregate.Apply(addedEvent);
        aggregate.Apply(valueUpdatedEvent);
        aggregate.Apply(soldEvent);

        // Assert - Verify final state after event replay
        aggregate.Id.Should().Be(investmentId.Value);
        aggregate.PortfolioId.Should().Be(portfolioId);
        aggregate.Symbol.Should().Be(symbol);
        aggregate.PurchasePrice.Should().Be(purchasePrice);
        aggregate.OriginalQuantity.Should().Be(quantity);
        aggregate.Quantity.Should().Be(0); // Sold all
        aggregate.Status.Should().Be(InvestmentStatus.Sold);
        aggregate.CurrentValue.Amount.Should().Be(0); // Complete sale sets value to 0
        aggregate.RealizedProfitLoss!.Amount.Should().Be((270m - 200m) * 5m); // 350
    }

    [Fact]
    public void EventSourcing_ShouldHandleMultipleValueUpdates()
    {
        // Arrange
        var aggregate = InvestmentAggregate.Create(
            _investmentId,
            _portfolioId,
            _symbol,
            _purchasePrice,
            _quantity,
            _purchaseDate);

        // Act - Multiple value updates (all positive values)
        aggregate.UpdateValue(new Money(110m, Currency.USD)); // +10%
        aggregate.UpdateValue(new Money(115m, Currency.USD)); // +5% from 110
        aggregate.UpdateValue(new Money(120m, Currency.USD)); // +5% from 115

        // Assert
        aggregate.CurrentValue.Amount.Should().Be(120m * _quantity); // 1200
        
        var events = aggregate.GetUncommittedEvents().ToList();
        events.Should().HaveCount(4); // 1 Added + 3 ValueUpdated
        events[0].Should().BeOfType<InvestmentAddedEvent>();
        events[1].Should().BeOfType<InvestmentValueUpdatedEvent>();
        events[2].Should().BeOfType<InvestmentValueUpdatedEvent>();
        events[3].Should().BeOfType<InvestmentValueUpdatedEvent>();
    }

    [Fact]
    public void EventSourcing_ShouldReconstructPartialSaleState()
    {
        // Arrange - Simulate partial sale sequence
        var aggregate = InvestmentAggregate.Create(
            _investmentId,
            _portfolioId,
            _symbol,
            _purchasePrice,
            10m,
            _purchaseDate);

        // Act
        aggregate.UpdateValue(new Money(150m, Currency.USD)); // Value increase
        aggregate.Sell(new Money(150m, Currency.USD), 6m, DateTime.UtcNow); // Partial sale

        // Assert
        aggregate.Status.Should().Be(InvestmentStatus.PartiallySold);
        aggregate.Quantity.Should().Be(4m);
        aggregate.OriginalQuantity.Should().Be(10m);
        aggregate.CurrentValue.Amount.Should().Be(150m * 4m); // 600
        
        // Realized profit from sale of 6 units: (150 - 100) * 6 = 300
        aggregate.RealizedProfitLoss!.Amount.Should().Be(300m);
    }

    #endregion

    #region Helper Methods Tests

    [Fact]
    public void GetUnrealizedProfitLoss_ShouldCalculateCorrectly_ForActiveInvestment()
    {
        // Arrange
        var aggregate = InvestmentAggregate.Create(
            _investmentId,
            _portfolioId,
            _symbol,
            new Money(100m, Currency.USD),
            10m,
            _purchaseDate);

        // Current value = 100 * 10 = 1000 (initial)
        // Act
        aggregate.UpdateValue(new Money(150m, Currency.USD)); // New value = 150 * 10 = 1500

        // Assert
        var unrealizedProfitLoss = aggregate.GetUnrealizedProfitLoss();
        unrealizedProfitLoss.Amount.Should().Be(500m); // 1500 - 1000
    }

    [Fact]
    public void GetUnrealizedProfitLoss_ShouldReturnZero_ForSoldInvestment()
    {
        // Arrange
        var aggregate = InvestmentAggregate.Create(
            _investmentId,
            _portfolioId,
            _symbol,
            _purchasePrice,
            _quantity,
            _purchaseDate);

        aggregate.Sell(new Money(120m, Currency.USD), null, DateTime.UtcNow);

        // Act
        var unrealizedProfitLoss = aggregate.GetUnrealizedProfitLoss();

        // Assert
        unrealizedProfitLoss.Amount.Should().Be(0); // No unrealized profit for sold investment
    }

    [Fact]
    public void GetROIPercentage_ShouldCalculateCorrectly_ForActiveInvestment()
    {
        // Arrange
        var aggregate = InvestmentAggregate.Create(
            _investmentId,
            _portfolioId,
            _symbol,
            new Money(100m, Currency.USD),
            10m,
            _purchaseDate);

        // Act
        aggregate.UpdateValue(new Money(125m, Currency.USD)); // 25% increase per unit

        // Assert
        var roi = aggregate.GetROIPercentage();
        roi.Should().Be(25m); // (1250 - 1000) / 1000 * 100 = 25%
    }

    [Fact]
    public void GetROIPercentage_ShouldCalculateCorrectly_ForSoldInvestment()
    {
        // Arrange
        var aggregate = InvestmentAggregate.Create(
            _investmentId,
            _portfolioId,
            _symbol,
            new Money(100m, Currency.USD),
            10m,
            _purchaseDate);

        // Act
        aggregate.Sell(new Money(120m, Currency.USD), null, DateTime.UtcNow);

        // Assert - ROI based on realized profit
        var roi = aggregate.GetROIPercentage();
        roi.Should().Be(20m); // (1200 - 1000) / 1000 * 100 = 20%
    }

    #endregion
}

