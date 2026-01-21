using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Enums;
using FluentAssertions;
using Xunit;
using System.ComponentModel.DataAnnotations;

namespace InvestmentHub.Domain.Tests.Commands;

/// <summary>
/// Unit tests for AddInvestmentCommand.
/// Tests the command structure and validation attributes.
/// </summary>
public class AddInvestmentCommandTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateCommand()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var symbol = new Symbol("AAPL", "NASDAQ", AssetType.Stock);
        var purchasePrice = new Money(150.00m, Currency.USD);
        var quantity = 10m;
        var purchaseDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var command = new AddInvestmentCommand(
            portfolioId,
            symbol,
            purchasePrice,
            quantity,
            purchaseDate);

        // Assert
        command.Should().NotBeNull();
        command.PortfolioId.Should().Be(portfolioId);
        command.Symbol.Should().Be(symbol);
        command.PurchasePrice.Should().Be(purchasePrice);
        command.Quantity.Should().Be(quantity);
        command.PurchaseDate.Should().Be(purchaseDate);
    }

    [Fact]
    public void Constructor_WithZeroQuantity_ShouldCreateCommand()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var symbol = new Symbol("AAPL", "NASDAQ", AssetType.Stock);
        var purchasePrice = new Money(150.00m, Currency.USD);
        var quantity = 0m; // Zero quantity
        var purchaseDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var command = new AddInvestmentCommand(
            portfolioId,
            symbol,
            purchasePrice,
            quantity,
            purchaseDate);

        // Assert
        command.Should().NotBeNull();
        command.Quantity.Should().Be(quantity);
    }

    [Fact]
    public void Constructor_WithDifferentAssetTypes_ShouldCreateCommand()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var symbol = new Symbol("BTC", "COINBASE", AssetType.Crypto);
        var purchasePrice = new Money(50000.00m, Currency.USD);
        var quantity = 0.1m;
        var purchaseDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var command = new AddInvestmentCommand(
            portfolioId,
            symbol,
            purchasePrice,
            quantity,
            purchaseDate);

        // Assert
        command.Should().NotBeNull();
        command.Symbol.AssetType.Should().Be(AssetType.Crypto);
    }

    [Fact]
    public void Constructor_WithDifferentCurrencies_ShouldCreateCommand()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var symbol = new Symbol("ASML", "EURONEXT", AssetType.Stock);
        var purchasePrice = new Money(800.00m, Currency.EUR);
        var quantity = 5m;
        var purchaseDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var command = new AddInvestmentCommand(
            portfolioId,
            symbol,
            purchasePrice,
            quantity,
            purchaseDate);

        // Assert
        command.Should().NotBeNull();
        command.PurchasePrice.Currency.Should().Be(Currency.EUR);
    }

    [Fact]
    public void Validation_WithValidData_ShouldPass()
    {
        // Arrange
        var command = new AddInvestmentCommand(
            PortfolioId.New(),
            new Symbol("AAPL", "NASDAQ", AssetType.Stock),
            new Money(150.00m, Currency.USD),
            10m,
            DateTime.UtcNow.AddDays(-1));

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(command);

        // Act
        var isValid = Validator.TryValidateObject(command, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void Validation_WithZeroQuantity_ShouldFail()
    {
        // Arrange
        var command = new AddInvestmentCommand(
            PortfolioId.New(),
            new Symbol("AAPL", "NASDAQ", AssetType.Stock),
            new Money(150.00m, Currency.USD),
            0m, // Invalid quantity
            DateTime.UtcNow.AddDays(-1));

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(command);

        // Act
        var isValid = Validator.TryValidateObject(command, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle();
        validationResults[0].ErrorMessage.Should().Contain("Quantity must be positive");
    }

    [Fact]
    public void Validation_WithMinValueDate_ShouldFail()
    {
        // Arrange
        var command = new AddInvestmentCommand(
            PortfolioId.New(),
            new Symbol("AAPL", "NASDAQ", AssetType.Stock),
            new Money(150.00m, Currency.USD),
            10m,
            DateTime.MinValue); // Invalid date

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(command);

        // Act
        var isValid = Validator.TryValidateObject(command, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle();
        validationResults[0].ErrorMessage.Should().Contain("Purchase date must be between 1900 and 2100");
    }
}
