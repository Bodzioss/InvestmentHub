using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace InvestmentHub.Domain.Tests.Queries;

/// <summary>
/// Unit tests for GetPortfolioQuery.
/// Tests the query structure and basic functionality.
/// </summary>
public class GetPortfolioQueryTests
{
    [Fact]
    public void Constructor_WithValidPortfolioId_ShouldCreateQuery()
    {
        // Arrange
        var portfolioId = PortfolioId.New();

        // Act
        var query = new GetPortfolioQuery(portfolioId);

        // Assert
        query.Should().NotBeNull();
        query.PortfolioId.Should().Be(portfolioId);
    }

    [Fact]
    public void Constructor_WithDifferentPortfolioIds_ShouldCreateDifferentQueries()
    {
        // Arrange
        var portfolioId1 = PortfolioId.New();
        var portfolioId2 = PortfolioId.New();

        // Act
        var query1 = new GetPortfolioQuery(portfolioId1);
        var query2 = new GetPortfolioQuery(portfolioId2);

        // Assert
        query1.Should().NotBeNull();
        query2.Should().NotBeNull();
        query1.PortfolioId.Should().NotBe(query2.PortfolioId);
    }

    [Fact]
    public void Constructor_WithSamePortfolioId_ShouldCreateEqualQueries()
    {
        // Arrange
        var portfolioId = PortfolioId.New();

        // Act
        var query1 = new GetPortfolioQuery(portfolioId);
        var query2 = new GetPortfolioQuery(portfolioId);

        // Assert
        query1.Should().NotBeNull();
        query2.Should().NotBeNull();
        query1.PortfolioId.Should().Be(query2.PortfolioId);
    }
}

/// <summary>
/// Unit tests for GetUserPortfoliosQuery.
/// Tests the query structure and basic functionality.
/// </summary>
public class GetUserPortfoliosQueryTests
{
    [Fact]
    public void Constructor_WithValidUserId_ShouldCreateQuery()
    {
        // Arrange
        var userId = UserId.New();

        // Act
        var query = new GetUserPortfoliosQuery(userId);

        // Assert
        query.Should().NotBeNull();
        query.UserId.Should().Be(userId);
    }

    [Fact]
    public void Constructor_WithDifferentUserIds_ShouldCreateDifferentQueries()
    {
        // Arrange
        var userId1 = UserId.New();
        var userId2 = UserId.New();

        // Act
        var query1 = new GetUserPortfoliosQuery(userId1);
        var query2 = new GetUserPortfoliosQuery(userId2);

        // Assert
        query1.Should().NotBeNull();
        query2.Should().NotBeNull();
        query1.UserId.Should().NotBe(query2.UserId);
    }

    [Fact]
    public void Constructor_WithSameUserId_ShouldCreateEqualQueries()
    {
        // Arrange
        var userId = UserId.New();

        // Act
        var query1 = new GetUserPortfoliosQuery(userId);
        var query2 = new GetUserPortfoliosQuery(userId);

        // Assert
        query1.Should().NotBeNull();
        query2.Should().NotBeNull();
        query1.UserId.Should().Be(query2.UserId);
    }
}

/// <summary>
/// Unit tests for GetInvestmentQuery.
/// Tests the query structure and basic functionality.
/// </summary>
public class GetInvestmentQueryTests
{
    [Fact]
    public void Constructor_WithValidInvestmentId_ShouldCreateQuery()
    {
        // Arrange
        var investmentId = InvestmentId.New();

        // Act
        var query = new GetInvestmentQuery(investmentId);

        // Assert
        query.Should().NotBeNull();
        query.InvestmentId.Should().Be(investmentId);
    }

    [Fact]
    public void Constructor_WithDifferentInvestmentIds_ShouldCreateDifferentQueries()
    {
        // Arrange
        var investmentId1 = InvestmentId.New();
        var investmentId2 = InvestmentId.New();

        // Act
        var query1 = new GetInvestmentQuery(investmentId1);
        var query2 = new GetInvestmentQuery(investmentId2);

        // Assert
        query1.Should().NotBeNull();
        query2.Should().NotBeNull();
        query1.InvestmentId.Should().NotBe(query2.InvestmentId);
    }

    [Fact]
    public void Constructor_WithSameInvestmentId_ShouldCreateEqualQueries()
    {
        // Arrange
        var investmentId = InvestmentId.New();

        // Act
        var query1 = new GetInvestmentQuery(investmentId);
        var query2 = new GetInvestmentQuery(investmentId);

        // Assert
        query1.Should().NotBeNull();
        query2.Should().NotBeNull();
        query1.InvestmentId.Should().Be(query2.InvestmentId);
    }
}
