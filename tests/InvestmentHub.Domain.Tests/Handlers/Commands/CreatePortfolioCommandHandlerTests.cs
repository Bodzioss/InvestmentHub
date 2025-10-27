using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.Handlers.Commands;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Repositories;
using FluentAssertions;
using Xunit;
using Moq;

namespace InvestmentHub.Domain.Tests.Handlers.Commands;

/// <summary>
/// Unit tests for CreatePortfolioCommandHandler.
/// Tests the business logic for creating new portfolios.
/// </summary>
public class CreatePortfolioCommandHandlerTests
{
    private readonly CreatePortfolioCommandHandler _handler;

    public CreatePortfolioCommandHandlerTests()
    {
        var portfolioRepository = new Mock<IPortfolioRepository>();
        var userRepository = new Mock<IUserRepository>();
        _handler = new CreatePortfolioCommandHandler(portfolioRepository.Object, userRepository.Object);
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
