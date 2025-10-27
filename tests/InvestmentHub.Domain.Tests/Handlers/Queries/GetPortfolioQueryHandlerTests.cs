using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.Handlers.Queries;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Repositories;
using FluentAssertions;
using Xunit;
using Moq;

namespace InvestmentHub.Domain.Tests.Handlers.Queries;

/// <summary>
/// Unit tests for GetPortfolioQueryHandler.
/// Tests the business logic for retrieving portfolio data.
/// </summary>
public class GetPortfolioQueryHandlerTests
{
    private readonly GetPortfolioQueryHandler _handler;

    public GetPortfolioQueryHandlerTests()
    {
        var portfolioRepository = new Mock<IPortfolioRepository>();
        var investmentRepository = new Mock<IInvestmentRepository>();
        _handler = new GetPortfolioQueryHandler(portfolioRepository.Object, investmentRepository.Object);
    }

    [Fact]
    public async Task Handle_WithValidQuery_ShouldReturnFailure()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var query = new GetPortfolioQuery(portfolioId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse(); // Currently returns failure as no repository is implemented
        result.ErrorMessage.Should().Contain("Portfolio not found");
        result.Portfolio.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithDifferentPortfolioIds_ShouldReturnFailure()
    {
        // Arrange
        var portfolioId1 = PortfolioId.New();
        var portfolioId2 = PortfolioId.New();
        
        var query1 = new GetPortfolioQuery(portfolioId1);
        var query2 = new GetPortfolioQuery(portfolioId2);

        // Act
        var result1 = await _handler.Handle(query1, CancellationToken.None);
        var result2 = await _handler.Handle(query2, CancellationToken.None);

        // Assert
        result1.Should().NotBeNull();
        result1.IsSuccess.Should().BeFalse();
        result1.ErrorMessage.Should().Contain("Portfolio not found");
        
        result2.Should().NotBeNull();
        result2.IsSuccess.Should().BeFalse();
        result2.ErrorMessage.Should().Contain("Portfolio not found");
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
}
