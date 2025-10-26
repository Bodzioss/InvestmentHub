using FluentAssertions;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.ValueObjects;
using Xunit;

namespace InvestmentHub.Domain.Tests.ValueObjects;

/// <summary>
/// Testy dla Money value object sprawdzające:
/// - Invarianty (nie może być ujemna kwota)
/// - Operacje matematyczne (dodawanie, odejmowanie, mnożenie)
/// - Równość i porównania
/// - Walidację walut
/// - Metody pomocnicze (Zero, ToString)
/// </summary>
public class MoneyTests
{
    [Fact]
    public void Constructor_WithValidAmount_ShouldCreateMoney()
    {
        // Arrange & Act
        var money = new Money(100.50m, Currency.USD);
        
        // Assert
        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be(Currency.USD);
    }
    
    [Fact]
    public void Constructor_WithZeroAmount_ShouldCreateMoney()
    {
        // Arrange & Act
        var money = new Money(0m, Currency.EUR);
        
        // Assert
        money.Amount.Should().Be(0m);
        money.Currency.Should().Be(Currency.EUR);
    }
    
    [Fact]
    public void Constructor_WithNegativeAmount_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => new Money(-10m, Currency.USD);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Money amount cannot be negative*");
    }
    
    [Fact]
    public void Add_WithSameCurrencyUSD_ShouldReturnCorrectSum()
    {
        // Arrange
        var money1 = new Money(100.50m, Currency.USD);
        var money2 = new Money(200.75m, Currency.USD);
        
        // Act
        var result = money1.Add(money2);
        
        // Assert
        result.Amount.Should().Be(301.25m);
        result.Currency.Should().Be(Currency.USD);
    }
    
    [Fact]
    public void Add_WithSameCurrencyEUR_ShouldReturnCorrectSum()
    {
        // Arrange
        var money1 = new Money(0m, Currency.EUR);
        var money2 = new Money(50m, Currency.EUR);
        
        // Act
        var result = money1.Add(money2);
        
        // Assert
        result.Amount.Should().Be(50m);
        result.Currency.Should().Be(Currency.EUR);
    }
    
    [Fact]
    public void Add_WithSameCurrencyGBP_ShouldReturnCorrectSum()
    {
        // Arrange
        var money1 = new Money(1000m, Currency.GBP);
        var money2 = new Money(0m, Currency.GBP);
        
        // Act
        var result = money1.Add(money2);
        
        // Assert
        result.Amount.Should().Be(1000m);
        result.Currency.Should().Be(Currency.GBP);
    }
    
    [Fact]
    public void Add_WithDifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var usdMoney = new Money(100m, Currency.USD);
        var eurMoney = new Money(100m, Currency.EUR);
        
        // Act
        var action = () => usdMoney.Add(eurMoney);
        
        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*different currencies*");
    }
    
    [Fact]
    public void Subtract_WithSameCurrencyUSD_ShouldReturnCorrectDifference()
    {
        // Arrange
        var money1 = new Money(200m, Currency.USD);
        var money2 = new Money(50m, Currency.USD);
        
        // Act
        var result = money1.Subtract(money2);
        
        // Assert
        result.Amount.Should().Be(150m);
        result.Currency.Should().Be(Currency.USD);
    }
    
    [Fact]
    public void Subtract_WithSameCurrencyEUR_ShouldReturnCorrectDifference()
    {
        // Arrange
        var money1 = new Money(100m, Currency.EUR);
        var money2 = new Money(100m, Currency.EUR);
        
        // Act
        var result = money1.Subtract(money2);
        
        // Assert
        result.Amount.Should().Be(0m);
        result.Currency.Should().Be(Currency.EUR);
    }
    
    [Fact]
    public void Subtract_WithSameCurrencyGBP_ShouldReturnCorrectDifference()
    {
        // Arrange
        var money1 = new Money(50m, Currency.GBP);
        var money2 = new Money(25m, Currency.GBP);
        
        // Act
        var result = money1.Subtract(money2);
        
        // Assert
        result.Amount.Should().Be(25m);
        result.Currency.Should().Be(Currency.GBP);
    }
    
    [Fact]
    public void Subtract_WithDifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var usdMoney = new Money(100m, Currency.USD);
        var eurMoney = new Money(100m, Currency.EUR);
        
        // Act
        var action = () => usdMoney.Subtract(eurMoney);
        
        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*different currencies*");
    }
    
    [Fact]
    public void Multiply_WithValidFactorUSD_ShouldReturnCorrectProduct()
    {
        // Arrange
        var money = new Money(100m, Currency.USD);
        
        // Act
        var result = money.Multiply(2.5m);
        
        // Assert
        result.Amount.Should().Be(250m);
        result.Currency.Should().Be(Currency.USD);
    }
    
    [Fact]
    public void Multiply_WithValidFactorEUR_ShouldReturnCorrectProduct()
    {
        // Arrange
        var money = new Money(50m, Currency.EUR);
        
        // Act
        var result = money.Multiply(0.5m);
        
        // Assert
        result.Amount.Should().Be(25m);
        result.Currency.Should().Be(Currency.EUR);
    }
    
    [Fact]
    public void Multiply_WithValidFactorGBP_ShouldReturnCorrectProduct()
    {
        // Arrange
        var money = new Money(200m, Currency.GBP);
        
        // Act
        var result = money.Multiply(1m);
        
        // Assert
        result.Amount.Should().Be(200m);
        result.Currency.Should().Be(Currency.GBP);
    }
    
    [Fact]
    public void Multiply_WithValidFactorJPY_ShouldReturnCorrectProduct()
    {
        // Arrange
        var money = new Money(100m, Currency.JPY);
        
        // Act
        var result = money.Multiply(0m);
        
        // Assert
        result.Amount.Should().Be(0m);
        result.Currency.Should().Be(Currency.JPY);
    }
    
    [Fact]
    public void Multiply_WithNegativeFactor_ShouldThrowArgumentException()
    {
        // Arrange
        var money = new Money(100m, Currency.USD);
        
        // Act
        var action = () => money.Multiply(-1m);
        
        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Multiplication factor cannot be negative*")
            .And.ParamName.Should().Be("factor");
    }
    
    [Fact]
    public void Zero_ShouldCreateMoneyWithZeroAmount()
    {
        // Arrange & Act
        var zeroMoney = Money.Zero(Currency.USD);
        
        // Assert
        zeroMoney.Amount.Should().Be(0m);
        zeroMoney.Currency.Should().Be(Currency.USD);
    }
    
    [Fact]
    public void Equals_WithSameAmountAndCurrency_ShouldReturnTrue()
    {
        // Arrange
        var money1 = new Money(100.50m, Currency.USD);
        var money2 = new Money(100.50m, Currency.USD);
        
        // Act & Assert
        money1.Should().Be(money2);
        money1.Equals(money2).Should().BeTrue();
        (money1 == money2).Should().BeTrue();
        (money1 != money2).Should().BeFalse();
    }
    
    [Fact]
    public void Equals_WithDifferentAmount_ShouldReturnFalse()
    {
        // Arrange
        var money1 = new Money(100m, Currency.USD);
        var money2 = new Money(200m, Currency.USD);
        
        // Act & Assert
        money1.Should().NotBe(money2);
        money1.Equals(money2).Should().BeFalse();
        (money1 == money2).Should().BeFalse();
        (money1 != money2).Should().BeTrue();
    }
    
    [Fact]
    public void Equals_WithDifferentCurrency_ShouldReturnFalse()
    {
        // Arrange
        var money1 = new Money(100m, Currency.USD);
        var money2 = new Money(100m, Currency.EUR);
        
        // Act & Assert
        money1.Should().NotBe(money2);
        money1.Equals(money2).Should().BeFalse();
    }
    
    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var money = new Money(100m, Currency.USD);
        
        // Act & Assert
        money.Equals(null).Should().BeFalse();
        (money == null).Should().BeFalse();
        (money != null).Should().BeTrue();
    }
    
    [Fact]
    public void GetHashCode_WithSameAmountAndCurrency_ShouldReturnSameHashCode()
    {
        // Arrange
        var money1 = new Money(100.50m, Currency.USD);
        var money2 = new Money(100.50m, Currency.USD);
        
        // Act & Assert
        money1.GetHashCode().Should().Be(money2.GetHashCode());
    }
    
    [Fact]
    public void GetHashCode_WithDifferentAmount_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var money1 = new Money(100m, Currency.USD);
        var money2 = new Money(200m, Currency.USD);
        
        // Act & Assert
        money1.GetHashCode().Should().NotBe(money2.GetHashCode());
    }
    
    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var money = new Money(123.45m, Currency.USD);
        
        // Act
        var result = money.ToString();
        
        // Assert
        result.Should().Contain("123,45"); // Polish locale uses comma as decimal separator
        result.Should().Contain("USD");
    }
}
