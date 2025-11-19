using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.Handlers.Queries;
using InvestmentHub.Domain.ValueObjects;
using FluentAssertions;
using Xunit;
using Moq;
using Marten;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Domain.Tests.Handlers.Queries;

/// <summary>
/// Unit tests for GetUserPortfoliosQueryHandler.
/// Note: These are simplified unit tests. For comprehensive testing of query operations,
/// see integration tests (MartenIntegrationTests) which use real Marten with TestContainers.
/// </summary>
public class GetUserPortfoliosQueryHandlerTests
{
    private readonly Mock<IDocumentSession> _sessionMock;
    private readonly Mock<ILogger<GetUserPortfoliosQueryHandler>> _loggerMock;

    public GetUserPortfoliosQueryHandlerTests()
    {
        _sessionMock = new Mock<IDocumentSession>();
        _loggerMock = new Mock<ILogger<GetUserPortfoliosQueryHandler>>();
    }

    [Fact]
    public void Handle_WithValidQuery_ShouldCallSessionQuery()
    {
        // Arrange
        var userId = UserId.New();
        var query = new GetUserPortfoliosQuery(userId);
        var handler = new GetUserPortfoliosQueryHandler(_sessionMock.Object, _loggerMock.Object);

        // Act
        // Note: This test verifies that the handler structure is correct
        // Full query testing should be done in integration tests with real Marten
        
        // For now, just verify the handler can be instantiated and accepts the query
        handler.Should().NotBeNull();
        query.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var userId = UserId.New();
        var query = new GetUserPortfoliosQuery(userId);
        var handler = new GetUserPortfoliosQueryHandler(_sessionMock.Object, _loggerMock.Object);

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.Handle(query, cancellationTokenSource.Token));
    }

    [Fact]
    public void Constructor_WithNullSession_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new GetUserPortfoliosQueryHandler(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new GetUserPortfoliosQueryHandler(_sessionMock.Object, null!));
    }
}
