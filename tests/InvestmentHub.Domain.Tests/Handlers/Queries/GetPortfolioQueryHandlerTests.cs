using FluentAssertions;
using InvestmentHub.Domain.Handlers.Queries;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.ReadModels;
using InvestmentHub.Domain.ValueObjects;
using Marten;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InvestmentHub.Domain.Tests.Handlers.Queries;

/// <summary>
/// Unit tests for GetPortfolioQueryHandler.
/// Tests the query logic for retrieving portfolios from read model.
/// </summary>
public class GetPortfolioQueryHandlerTests
{
    private readonly GetPortfolioQueryHandler _handler;
    private readonly Mock<IDocumentSession> _sessionMock;
    private readonly Mock<ILogger<GetPortfolioQueryHandler>> _loggerMock;

    public GetPortfolioQueryHandlerTests()
    {
        _sessionMock = new Mock<IDocumentSession>();
        _loggerMock = new Mock<ILogger<GetPortfolioQueryHandler>>();

        _handler = new GetPortfolioQueryHandler(
            _sessionMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidPortfolioId_ShouldReturnSuccess()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        
        var portfolioReadModel = new PortfolioReadModel
        {
            Id = portfolioId.Value,
            OwnerId = ownerId.Value,
            Name = "Test Portfolio",
            Description = "Test Description",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            IsClosed = false,
            TotalValue = 10000,
            Currency = "USD",
            InvestmentCount = 5,
            LastUpdated = DateTime.UtcNow,
            Version = 1
        };

        _sessionMock
            .Setup(x => x.LoadAsync<PortfolioReadModel>(portfolioId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(portfolioReadModel);

        var query = new GetPortfolioQuery(portfolioId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Portfolio.Should().NotBeNull();
        result.Portfolio!.Id.Should().Be(portfolioId.Value);
        result.Portfolio.Name.Should().Be("Test Portfolio");
        result.Portfolio.TotalValue.Should().Be(10000);
        result.Portfolio.InvestmentCount.Should().Be(5);
        result.ErrorMessage.Should().BeNull();

        _sessionMock.Verify(
            x => x.LoadAsync<PortfolioReadModel>(portfolioId.Value, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentPortfolio_ShouldReturnFailure()
    {
        // Arrange
        var portfolioId = PortfolioId.New();

        _sessionMock
            .Setup(x => x.LoadAsync<PortfolioReadModel>(portfolioId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PortfolioReadModel?)null);

        var query = new GetPortfolioQuery(portfolioId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Portfolio.Should().BeNull();
        result.ErrorMessage.Should().Be("Portfolio not found");

        _sessionMock.Verify(
            x => x.LoadAsync<PortfolioReadModel>(portfolioId.Value, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithClosedPortfolio_ShouldReturnSuccess()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        
        var portfolioReadModel = new PortfolioReadModel
        {
            Id = portfolioId.Value,
            OwnerId = ownerId.Value,
            Name = "Closed Portfolio",
            Description = "This portfolio is closed",
            CreatedAt = DateTime.UtcNow.AddDays(-100),
            IsClosed = true,
            ClosedAt = DateTime.UtcNow.AddDays(-1),
            CloseReason = "User request",
            TotalValue = 0,
            Currency = "USD",
            InvestmentCount = 0,
            LastUpdated = DateTime.UtcNow.AddDays(-1),
            Version = 3
        };

        _sessionMock
            .Setup(x => x.LoadAsync<PortfolioReadModel>(portfolioId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(portfolioReadModel);

        var query = new GetPortfolioQuery(portfolioId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Portfolio.Should().NotBeNull();
        result.Portfolio!.IsClosed.Should().BeTrue();
        result.Portfolio.CloseReason.Should().Be("User request");
        result.Portfolio.InvestmentCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var query = new GetPortfolioQuery(portfolioId);

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _handler.Handle(query, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task Handle_WhenSessionThrowsException_ShouldReturnFailure()
    {
        // Arrange
        var portfolioId = PortfolioId.New();

        _sessionMock
            .Setup(x => x.LoadAsync<PortfolioReadModel>(portfolioId.Value, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        var query = new GetPortfolioQuery(portfolioId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Portfolio.Should().BeNull();
        result.ErrorMessage.Should().Contain("Failed to retrieve portfolio");
        result.ErrorMessage.Should().Contain("Database connection failed");
    }

    [Fact]
    public async Task Handle_WithDifferentVersions_ShouldReturnCorrectVersion()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        
        var portfolioReadModel = new PortfolioReadModel
        {
            Id = portfolioId.Value,
            OwnerId = ownerId.Value,
            Name = "Versioned Portfolio",
            Description = "Portfolio with version",
            CreatedAt = DateTime.UtcNow.AddDays(-50),
            IsClosed = false,
            TotalValue = 25000,
            Currency = "USD",
            InvestmentCount = 10,
            LastUpdated = DateTime.UtcNow,
            Version = 7 // This portfolio has been modified 7 times
        };

        _sessionMock
            .Setup(x => x.LoadAsync<PortfolioReadModel>(portfolioId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(portfolioReadModel);

        var query = new GetPortfolioQuery(portfolioId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Portfolio.Should().NotBeNull();
        result.Portfolio!.Version.Should().Be(7);
    }

    [Fact]
    public async Task Handle_WithNullDescription_ShouldReturnSuccess()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        
        var portfolioReadModel = new PortfolioReadModel
        {
            Id = portfolioId.Value,
            OwnerId = ownerId.Value,
            Name = "Portfolio Without Description",
            Description = null, // No description
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            IsClosed = false,
            TotalValue = 5000,
            Currency = "USD",
            InvestmentCount = 2,
            LastUpdated = DateTime.UtcNow,
            Version = 1
        };

        _sessionMock
            .Setup(x => x.LoadAsync<PortfolioReadModel>(portfolioId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(portfolioReadModel);

        var query = new GetPortfolioQuery(portfolioId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Portfolio.Should().NotBeNull();
        result.Portfolio!.Description.Should().BeNull();
    }
}
