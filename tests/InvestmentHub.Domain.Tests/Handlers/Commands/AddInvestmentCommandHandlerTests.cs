using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.Handlers.Commands;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Services;
using FluentAssertions;
using Xunit;
using Moq;
using Marten;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Domain.Tests.Handlers.Commands;

/// <summary>
/// Unit tests for AddInvestmentCommandHandler.
/// Tests the business logic for adding new investments to portfolios with Marten event sourcing.
/// </summary>
public class AddInvestmentCommandHandlerTests
{
    private readonly AddInvestmentCommandHandler _handler;
    private readonly Mock<IDocumentSession> _sessionMock;
    private readonly Mock<ILogger<AddInvestmentCommandHandler>> _loggerMock;
    private readonly Mock<ICorrelationIdEnricher> _correlationIdEnricherMock;
    private readonly Mock<IMetricsRecorder> _metricsRecorderMock;

    public AddInvestmentCommandHandlerTests()
    {
        _sessionMock = new Mock<IDocumentSession>();
        _loggerMock = new Mock<ILogger<AddInvestmentCommandHandler>>();
        _correlationIdEnricherMock = new Mock<ICorrelationIdEnricher>();
        _metricsRecorderMock = new Mock<IMetricsRecorder>();

        // Setup Marten event store mock
        var eventStoreMock = new Mock<Marten.Events.IEventStore>();
        _sessionMock
            .Setup(x => x.Events)
            .Returns(eventStoreMock.Object);

        _sessionMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new AddInvestmentCommandHandler(
            _sessionMock.Object,
            _loggerMock.Object,
            _correlationIdEnricherMock.Object,
            _metricsRecorderMock.Object);
    }

    [Fact]
    public async Task Handle_WithNegativeQuantity_ShouldReturnFailure()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var symbol = new Symbol("AAPL", "NASDAQ", AssetType.Stock);
        var purchasePrice = new Money(150.00m, Currency.USD);
        var quantity = -10m; // Invalid negative quantity
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
    public async Task Handle_WithZeroQuantity_ShouldReturnFailure()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var symbol = new Symbol("AAPL", "NASDAQ", AssetType.Stock);
        var purchasePrice = new Money(150.00m, Currency.USD);
        var quantity = 0m; // Invalid zero quantity
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

        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _handler.Handle(command, cancellationTokenSource.Token));
    }
}
