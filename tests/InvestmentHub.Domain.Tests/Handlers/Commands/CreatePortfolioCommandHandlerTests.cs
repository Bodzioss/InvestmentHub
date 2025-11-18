using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.Handlers.Commands;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Repositories;
using InvestmentHub.Domain.Entities;
using FluentAssertions;
using Xunit;
using Moq;
using Marten;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Domain.Tests.Handlers.Commands;

/// <summary>
/// Unit tests for CreatePortfolioCommandHandler.
/// Tests the business logic for creating new portfolios with Marten event sourcing.
/// </summary>
public class CreatePortfolioCommandHandlerTests
{
    private readonly CreatePortfolioCommandHandler _handler;
    private readonly Mock<IDocumentSession> _sessionMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPortfolioRepository> _portfolioRepositoryMock;
    private readonly Mock<ILogger<CreatePortfolioCommandHandler>> _loggerMock;

    public CreatePortfolioCommandHandlerTests()
    {
        _sessionMock = new Mock<IDocumentSession>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _portfolioRepositoryMock = new Mock<IPortfolioRepository>();
        _loggerMock = new Mock<ILogger<CreatePortfolioCommandHandler>>();

        // Setup default successful responses
        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User(UserId.New(), "Test User", "test@test.com", DateTime.UtcNow));
        
        _userRepositoryMock
            .Setup(x => x.CanCreatePortfolioAsync(It.IsAny<UserId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        _portfolioRepositoryMock
            .Setup(x => x.ExistsByNameAsync(It.IsAny<UserId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Setup Marten event store mock
        var eventStoreMock = new Mock<Marten.Events.IEventStore>();
        _sessionMock
            .Setup(x => x.Events)
            .Returns(eventStoreMock.Object);
        
        _sessionMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new CreatePortfolioCommandHandler(
            _sessionMock.Object,
            _userRepositoryMock.Object,
            _portfolioRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnSuccess()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        var name = "My Investment Portfolio";
        var description = "A diversified investment portfolio";

        var command = new CreatePortfolioCommand(
            portfolioId,
            ownerId,
            name,
            description);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.PortfolioId.Should().Be(portfolioId);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithEmptyName_ShouldReturnFailure()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        var name = ""; // Invalid name
        var description = "A diversified investment portfolio";

        var command = new CreatePortfolioCommand(
            portfolioId,
            ownerId,
            name,
            description);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Portfolio name cannot be empty");
        result.PortfolioId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithWhitespaceName_ShouldReturnFailure()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        var name = "   "; // Invalid name
        var description = "A diversified investment portfolio";

        var command = new CreatePortfolioCommand(
            portfolioId,
            ownerId,
            name,
            description);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Portfolio name cannot be empty");
        result.PortfolioId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNullDescription_ShouldReturnSuccess()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        var name = "My Investment Portfolio";
        string? description = null; // Null description should be allowed

        var command = new CreatePortfolioCommand(
            portfolioId,
            ownerId,
            name,
            description);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.PortfolioId.Should().Be(portfolioId);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithLongName_ShouldReturnSuccess()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        var name = "This is a very long portfolio name that should still be valid";
        var description = "A diversified investment portfolio";

        var command = new CreatePortfolioCommand(
            portfolioId,
            ownerId,
            name,
            description);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.PortfolioId.Should().Be(portfolioId);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        var name = "My Investment Portfolio";
        var description = "A diversified investment portfolio";

        var command = new CreatePortfolioCommand(
            portfolioId,
            ownerId,
            name,
            description);

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _handler.Handle(command, cancellationTokenSource.Token));
    }
}
