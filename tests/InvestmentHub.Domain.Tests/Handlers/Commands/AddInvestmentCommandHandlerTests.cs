using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.Handlers.Commands;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Repositories;
using FluentAssertions;
using Xunit;
using Moq;

namespace InvestmentHub.Domain.Tests.Handlers.Commands;

/// <summary>
/// Unit tests for AddInvestmentCommandHandler.
/// Tests the business logic for adding investments to portfolios.
/// </summary>
public class AddInvestmentCommandHandlerTests
{
    private readonly AddInvestmentCommandHandler _handler;

    public AddInvestmentCommandHandlerTests()
    {
        var portfolioRepository = new Mock<IPortfolioRepository>();
        var investmentRepository = new Mock<IInvestmentRepository>();
        var userRepository = new Mock<IUserRepository>();
        _handler = new AddInvestmentCommandHandler(portfolioRepository.Object, investmentRepository.Object, userRepository.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnSuccess()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var symbol = new Symbol("AAPL", "NASDAQ", AssetType.Stock);
        var purchasePrice = new Money(150.00m, Currency.USD);
        var quantity = 10m;
        var purchaseDate = DateTime.UtcNow.AddDays(-1);

        var command = new AddInvestmentCommand(
            portfolioId,
            symbol,
            purchasePrice,
            quantity,
            purchaseDate);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.InvestmentId.Should().NotBeNull();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithZeroQuantity_ShouldReturnFailure()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var symbol = new Symbol("AAPL", "NASDAQ", AssetType.Stock);
        var purchasePrice = new Money(150.00m, Currency.USD);
        var quantity = 0m; // Invalid quantity
        var purchaseDate = DateTime.UtcNow.AddDays(-1);

        var command = new AddInvestmentCommand(
            portfolioId,
            symbol,
            purchasePrice,
            quantity,
            purchaseDate);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Quantity must be positive");
        result.InvestmentId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNegativeQuantity_ShouldReturnFailure()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var symbol = new Symbol("AAPL", "NASDAQ", AssetType.Stock);
        var purchasePrice = new Money(150.00m, Currency.USD);
        var quantity = -5m; // Invalid quantity
        var purchaseDate = DateTime.UtcNow.AddDays(-1);

        var command = new AddInvestmentCommand(
            portfolioId,
            symbol,
            purchasePrice,
            quantity,
            purchaseDate);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Quantity must be positive");
        result.InvestmentId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithFuturePurchaseDate_ShouldReturnFailure()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var symbol = new Symbol("AAPL", "NASDAQ", AssetType.Stock);
        var purchasePrice = new Money(150.00m, Currency.USD);
        var quantity = 10m;
        var purchaseDate = DateTime.UtcNow.AddDays(1); // Future date

        var command = new AddInvestmentCommand(
            portfolioId,
            symbol,
            purchasePrice,
            quantity,
            purchaseDate);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Purchase date cannot be in the future");
        result.InvestmentId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithDifferentAssetTypes_ShouldReturnSuccess()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var symbol = new Symbol("BTC", "COINBASE", AssetType.Crypto);
        var purchasePrice = new Money(50000.00m, Currency.USD);
        var quantity = 0.1m;
        var purchaseDate = DateTime.UtcNow.AddDays(-1);

        var command = new AddInvestmentCommand(
            portfolioId,
            symbol,
            purchasePrice,
            quantity,
            purchaseDate);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.InvestmentId.Should().NotBeNull();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithDifferentCurrencies_ShouldReturnSuccess()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var symbol = new Symbol("ASML", "EURONEXT", AssetType.Stock);
        var purchasePrice = new Money(800.00m, Currency.EUR);
        var quantity = 5m;
        var purchaseDate = DateTime.UtcNow.AddDays(-1);

        var command = new AddInvestmentCommand(
            portfolioId,
            symbol,
            purchasePrice,
            quantity,
            purchaseDate);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.InvestmentId.Should().NotBeNull();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var symbol = new Symbol("AAPL", "NASDAQ", AssetType.Stock);
        var purchasePrice = new Money(150.00m, Currency.USD);
        var quantity = 10m;
        var purchaseDate = DateTime.UtcNow.AddDays(-1);

        var command = new AddInvestmentCommand(
            portfolioId,
            symbol,
            purchasePrice,
            quantity,
            purchaseDate);

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _handler.Handle(command, cancellationTokenSource.Token));
    }
}
