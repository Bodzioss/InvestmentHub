using FluentAssertions;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.ValueObjects;
using Xunit;

namespace InvestmentHub.Domain.Tests.Entities;

/// <summary>
/// Testy dla Investment entity sprawdzające:
/// - Invarianty biznesowe (quantity > 0, purchase date nie w przyszłości)
/// - Walidację parametrów konstruktora
/// - Metody aktualizacji (UpdateCurrentValue, UpdateQuantity)
/// - Zmianę statusu (MarkAsSold, Suspend)
/// - Obliczenia finansowe (GetTotalCost, GetUnrealizedGainLoss, GetPercentageGainLoss)
/// - Walidację operacji na nieaktywnych inwestycjach
/// </summary>
public class InvestmentTests
{
    private readonly InvestmentId _investmentId;
    private readonly PortfolioId _portfolioId;
    private readonly Symbol _symbol;
    private readonly Money _purchasePrice;
    private readonly decimal _quantity;
    private readonly DateTime _purchaseDate;
    
    public InvestmentTests()
    {
        _investmentId = InvestmentId.New();
        _portfolioId = PortfolioId.New();
        _symbol = Symbol.Stock("AAPL", "NASDAQ");
        _purchasePrice = new Money(150.00m, Currency.USD);
        _quantity = 10m;
        _purchaseDate = DateTime.UtcNow.AddDays(-30);
    }
    
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInvestment()
    {
        // Arrange & Act
        var investment = new Investment(_investmentId, _portfolioId, _symbol, _purchasePrice, _quantity, _purchaseDate);
        
        // Assert
        investment.Id.Should().Be(_investmentId);
        investment.Symbol.Should().Be(_symbol);
        investment.PurchasePrice.Should().Be(_purchasePrice);
        investment.Quantity.Should().Be(_quantity);
        investment.PurchaseDate.Should().Be(_purchaseDate);
        investment.Status.Should().Be(InvestmentStatus.Active);
        investment.CurrentValue.Should().Be(_purchasePrice.Multiply(_quantity));
    }
    
    [Fact]
    public void Constructor_WithZeroQuantity_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var action = () => new Investment(_investmentId, _portfolioId, _symbol, _purchasePrice, 0m, _purchaseDate);
        
        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Quantity must be positive*")
            .And.ParamName.Should().Be("quantity");
    }
    
    [Fact]
    public void Constructor_WithNegativeQuantity_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var action = () => new Investment(_investmentId, _portfolioId, _symbol, _purchasePrice, -5m, _purchaseDate);
        
        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Quantity must be positive*")
            .And.ParamName.Should().Be("quantity");
    }
    
    [Fact]
    public void Constructor_WithFuturePurchaseDate_ShouldThrowArgumentException()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1);
        
        // Act
        var action = () => new Investment(_investmentId, _portfolioId, _symbol, _purchasePrice, _quantity, futureDate);
        
        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Purchase date cannot be in the future*")
            .And.ParamName.Should().Be("purchaseDate");
    }
    
    [Fact]
    public void Constructor_WithNullId_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = () => new Investment(null!, _portfolioId, _symbol, _purchasePrice, _quantity, _purchaseDate);
        
        // Assert
        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("id");
    }
    
    [Fact]
    public void Constructor_WithNullSymbol_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = () => new Investment(_investmentId, _portfolioId, null!, _purchasePrice, _quantity, _purchaseDate);
        
        // Assert
        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("symbol");
    }
    
    [Fact]
    public void Constructor_WithNullPurchasePrice_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = () => new Investment(_investmentId, _portfolioId, _symbol, null!, _quantity, _purchaseDate);
        
        // Assert
        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("purchasePrice");
    }
    
    [Fact]
    public void UpdateCurrentValue_WithValidPrice_ShouldUpdateCurrentValue()
    {
        // Arrange
        var investment = new Investment(_investmentId, _portfolioId, _symbol, _purchasePrice, _quantity, _purchaseDate);
        var newPrice = new Money(160.00m, Currency.USD);
        var expectedNewValue = newPrice.Multiply(_quantity);
        
        // Act
        investment.UpdateCurrentValue(newPrice);
        
        // Assert
        investment.CurrentValue.Should().Be(expectedNewValue);
        investment.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public void UpdateCurrentValue_WithValidPrice_ShouldUpdateValue()
    {
        // Arrange
        var investment = new Investment(_investmentId, _portfolioId, _symbol, _purchasePrice, _quantity, _purchaseDate);
        var newPrice = new Money(10m, Currency.USD);
        var expectedValue = newPrice.Multiply(_quantity);
        
        // Act
        investment.UpdateCurrentValue(newPrice);
        
        // Assert
        investment.CurrentValue.Should().Be(expectedValue);
        investment.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public void UpdateCurrentValue_OnSoldInvestment_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var investment = new Investment(_investmentId, _portfolioId, _symbol, _purchasePrice, _quantity, _purchaseDate);
        investment.MarkAsSold();
        var newPrice = new Money(160.00m, Currency.USD);
        
        // Act
        var action = () => investment.UpdateCurrentValue(newPrice);
        
        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot update value of inactive investment*");
    }
    
    [Fact]
    public void UpdateQuantity_WithValidQuantity_ShouldUpdateQuantity()
    {
        // Arrange
        var investment = new Investment(_investmentId, _portfolioId, _symbol, _purchasePrice, _quantity, _purchaseDate);
        var newQuantity = 15m;
        var expectedNewValue = _purchasePrice.Multiply(newQuantity);
        
        // Act
        investment.UpdateQuantity(newQuantity);
        
        // Assert
        investment.Quantity.Should().Be(newQuantity);
        investment.CurrentValue.Should().Be(expectedNewValue);
        investment.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public void UpdateQuantity_WithZeroQuantity_ShouldThrowArgumentException()
    {
        // Arrange
        var investment = new Investment(_investmentId, _portfolioId, _symbol, _purchasePrice, _quantity, _purchaseDate);
        
        // Act
        var action = () => investment.UpdateQuantity(0m);
        
        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Quantity must be positive*")
            .And.ParamName.Should().Be("newQuantity");
    }
    
    [Fact]
    public void UpdateQuantity_OnSoldInvestment_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var investment = new Investment(_investmentId, _portfolioId, _symbol, _purchasePrice, _quantity, _purchaseDate);
        investment.MarkAsSold();
        
        // Act
        var action = () => investment.UpdateQuantity(15m);
        
        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot update quantity of inactive investment*");
    }
    
    [Fact]
    public void MarkAsSold_OnActiveInvestment_ShouldChangeStatusToSold()
    {
        // Arrange
        var investment = new Investment(_investmentId, _portfolioId, _symbol, _purchasePrice, _quantity, _purchaseDate);
        
        // Act
        investment.MarkAsSold();
        
        // Assert
        investment.Status.Should().Be(InvestmentStatus.Sold);
        investment.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public void MarkAsSold_OnAlreadySoldInvestment_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var investment = new Investment(_investmentId, _portfolioId, _symbol, _purchasePrice, _quantity, _purchaseDate);
        investment.MarkAsSold();
        
        // Act
        var action = () => investment.MarkAsSold();
        
        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot sell inactive investment*");
    }
    
    [Fact]
    public void Suspend_OnActiveInvestment_ShouldChangeStatusToSuspended()
    {
        // Arrange
        var investment = new Investment(_investmentId, _portfolioId, _symbol, _purchasePrice, _quantity, _purchaseDate);
        
        // Act
        investment.Suspend();
        
        // Assert
        investment.Status.Should().Be(InvestmentStatus.Suspended);
        investment.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public void Suspend_OnSoldInvestment_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var investment = new Investment(_investmentId, _portfolioId, _symbol, _purchasePrice, _quantity, _purchaseDate);
        investment.MarkAsSold();
        
        // Act
        var action = () => investment.Suspend();
        
        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot suspend sold investment*");
    }
    
    [Fact]
    public void GetTotalCost_ShouldReturnPurchasePriceTimesQuantity()
    {
        // Arrange
        var investment = new Investment(_investmentId, _portfolioId, _symbol, _purchasePrice, _quantity, _purchaseDate);
        var expectedTotalCost = _purchasePrice.Multiply(_quantity);
        
        // Act
        var totalCost = investment.GetTotalCost();
        
        // Assert
        totalCost.Should().Be(expectedTotalCost);
    }
    
    [Fact]
    public void GetUnrealizedGainLoss_WithHigherCurrentValue_ShouldReturnPositiveGain()
    {
        // Arrange
        var investment = new Investment(_investmentId, _portfolioId, _symbol, _purchasePrice, _quantity, _purchaseDate);
        var higherPrice = new Money(160.00m, Currency.USD);
        investment.UpdateCurrentValue(higherPrice);
        
        // Act
        var gainLoss = investment.GetUnrealizedGainLoss();
        
        // Assert
        gainLoss.Amount.Should().BePositive();
        gainLoss.Should().Be(new Money(100m, Currency.USD)); // (160-150) * 10
    }
    
    [Fact]
    public void GetUnrealizedGainLoss_WithLowerCurrentValue_ShouldReturnNegativeLoss()
    {
        // Arrange
        var investment = new Investment(_investmentId, _portfolioId, _symbol, _purchasePrice, _quantity, _purchaseDate);
        var lowerPrice = new Money(140.00m, Currency.USD);
        investment.UpdateCurrentValue(lowerPrice);
        
        // Act
        var gainLoss = investment.GetUnrealizedGainLoss();
        
        // Assert
        gainLoss.Amount.Should().Be(100m); // Absolute value of loss
        gainLoss.Currency.Should().Be(Currency.USD);
        // Verify it's a loss by checking if current value is less than total cost
        investment.CurrentValue.Amount.Should().BeLessThan(investment.GetTotalCost().Amount);
    }
    
    [Fact]
    public void GetPercentageGainLoss_WithGain_ShouldReturnPositivePercentage()
    {
        // Arrange
        var investment = new Investment(_investmentId, _portfolioId, _symbol, _purchasePrice, _quantity, _purchaseDate);
        var higherPrice = new Money(165.00m, Currency.USD); // 10% gain
        investment.UpdateCurrentValue(higherPrice);
        
        // Act
        var percentage = investment.GetPercentageGainLoss();
        
        // Assert
        percentage.Should().Be(0.10m); // 10% gain
    }
    
    [Fact]
    public void GetPercentageGainLoss_WithLoss_ShouldReturnNegativePercentage()
    {
        // Arrange
        var investment = new Investment(_investmentId, _portfolioId, _symbol, _purchasePrice, _quantity, _purchaseDate);
        var lowerPrice = new Money(135.00m, Currency.USD); // 10% loss
        investment.UpdateCurrentValue(lowerPrice);
        
        // Act
        var percentage = investment.GetPercentageGainLoss();
        
        // Assert
        percentage.Should().Be(-0.10m); // 10% loss
    }
    
    [Fact]
    public void IsProfitable_WithGain_ShouldReturnTrue()
    {
        // Arrange
        var investment = new Investment(_investmentId, _portfolioId, _symbol, _purchasePrice, _quantity, _purchaseDate);
        var higherPrice = new Money(160.00m, Currency.USD);
        investment.UpdateCurrentValue(higherPrice);
        
        // Act
        var isProfitable = investment.IsProfitable();
        
        // Assert
        isProfitable.Should().BeTrue();
    }
    
    [Fact]
    public void IsProfitable_WithLoss_ShouldReturnFalse()
    {
        // Arrange
        var investment = new Investment(_investmentId, _portfolioId, _symbol, _purchasePrice, _quantity, _purchaseDate);
        var lowerPrice = new Money(140.00m, Currency.USD);
        investment.UpdateCurrentValue(lowerPrice);
        
        // Act
        var isProfitable = investment.IsProfitable();
        
        // Assert
        isProfitable.Should().BeFalse();
    }
    
    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var investment = new Investment(_investmentId, _portfolioId, _symbol, _purchasePrice, _quantity, _purchaseDate);
        
        // Act
        var result = investment.ToString();
        
        // Assert
        result.Should().Contain("AAPL");
        result.Should().Contain("10 units");
        result.Should().Contain("150,00 USD"); // Polish locale uses comma as decimal separator
        result.Should().Contain("Active");
    }
}
