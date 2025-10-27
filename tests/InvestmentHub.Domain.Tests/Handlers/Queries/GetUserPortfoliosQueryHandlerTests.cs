using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.Handlers.Queries;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Repositories;
using FluentAssertions;
using Xunit;
using Moq;

namespace InvestmentHub.Domain.Tests.Handlers.Queries;

/// <summary>
/// Unit tests for GetUserPortfoliosQueryHandler.
/// Tests the business logic for retrieving user portfolios.
/// </summary>
public class GetUserPortfoliosQueryHandlerTests
{
    private readonly GetUserPortfoliosQueryHandler _handler;

    public GetUserPortfoliosQueryHandlerTests()
    {
        var portfolioRepository = new Mock<IPortfolioRepository>();
        var userRepository = new Mock<IUserRepository>();
        _handler = new GetUserPortfoliosQueryHandler(portfolioRepository.Object, userRepository.Object);
    }

    [Fact]
    public async Task Handle_WithValidQuery_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = UserId.New();
        var query = new GetUserPortfoliosQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Portfolios.Should().NotBeNull();
        result.Portfolios.Should().BeEmpty(); // Currently returns empty list as no repository is implemented
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithDifferentUserIds_ShouldReturnEmptyLists()
    {
        // Arrange
        var userId1 = UserId.New();
        var userId2 = UserId.New();
        
        var query1 = new GetUserPortfoliosQuery(userId1);
        var query2 = new GetUserPortfoliosQuery(userId2);

        // Act
        var result1 = await _handler.Handle(query1, CancellationToken.None);
        var result2 = await _handler.Handle(query2, CancellationToken.None);

        // Assert
        result1.Should().NotBeNull();
        result1.IsSuccess.Should().BeTrue();
        result1.Portfolios.Should().BeEmpty();
        
        result2.Should().NotBeNull();
        result2.IsSuccess.Should().BeTrue();
        result2.Portfolios.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var userId = UserId.New();
        var query = new GetUserPortfoliosQuery(userId);

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _handler.Handle(query, cancellationTokenSource.Token));
    }
}
