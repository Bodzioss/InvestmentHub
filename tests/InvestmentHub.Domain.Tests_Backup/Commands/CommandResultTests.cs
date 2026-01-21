using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace InvestmentHub.Domain.Tests.Commands;

/// <summary>
/// Unit tests for AddInvestmentResult.
/// Tests the result structure and factory methods.
/// </summary>
public class AddInvestmentResultTests
{
    [Fact]
    public void Success_WithValidInvestmentId_ShouldCreateSuccessResult()
    {
        // Arrange
        var investmentId = InvestmentId.New();

        // Act
        var result = AddInvestmentResult.Success(investmentId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.InvestmentId.Should().Be(investmentId);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Failure_WithErrorMessage_ShouldCreateFailureResult()
    {
        // Arrange
        var errorMessage = "Test error message";

        // Act
        var result = AddInvestmentResult.Failure(errorMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.InvestmentId.Should().BeNull();
        result.ErrorMessage.Should().Be(errorMessage);
    }

    [Fact]
    public void Success_WithDifferentInvestmentIds_ShouldCreateDifferentResults()
    {
        // Arrange
        var investmentId1 = InvestmentId.New();
        var investmentId2 = InvestmentId.New();

        // Act
        var result1 = AddInvestmentResult.Success(investmentId1);
        var result2 = AddInvestmentResult.Success(investmentId2);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.InvestmentId.Should().NotBe(result2.InvestmentId);
    }

    [Fact]
    public void Failure_WithDifferentErrorMessages_ShouldCreateDifferentResults()
    {
        // Arrange
        var errorMessage1 = "First error message";
        var errorMessage2 = "Second error message";

        // Act
        var result1 = AddInvestmentResult.Failure(errorMessage1);
        var result2 = AddInvestmentResult.Failure(errorMessage2);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.ErrorMessage.Should().NotBe(result2.ErrorMessage);
    }
}

/// <summary>
/// Unit tests for CreatePortfolioResult.
/// Tests the result structure and factory methods.
/// </summary>
public class CreatePortfolioResultTests
{
    [Fact]
    public void Success_WithValidPortfolioId_ShouldCreateSuccessResult()
    {
        // Arrange
        var portfolioId = PortfolioId.New();

        // Act
        var result = CreatePortfolioResult.Success(portfolioId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.PortfolioId.Should().Be(portfolioId);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Failure_WithErrorMessage_ShouldCreateFailureResult()
    {
        // Arrange
        var errorMessage = "Test error message";

        // Act
        var result = CreatePortfolioResult.Failure(errorMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.PortfolioId.Should().BeNull();
        result.ErrorMessage.Should().Be(errorMessage);
    }

    [Fact]
    public void Success_WithDifferentPortfolioIds_ShouldCreateDifferentResults()
    {
        // Arrange
        var portfolioId1 = PortfolioId.New();
        var portfolioId2 = PortfolioId.New();

        // Act
        var result1 = CreatePortfolioResult.Success(portfolioId1);
        var result2 = CreatePortfolioResult.Success(portfolioId2);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.PortfolioId.Should().NotBe(result2.PortfolioId);
    }

    [Fact]
    public void Failure_WithDifferentErrorMessages_ShouldCreateDifferentResults()
    {
        // Arrange
        var errorMessage1 = "First error message";
        var errorMessage2 = "Second error message";

        // Act
        var result1 = CreatePortfolioResult.Failure(errorMessage1);
        var result2 = CreatePortfolioResult.Failure(errorMessage2);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.ErrorMessage.Should().NotBe(result2.ErrorMessage);
    }
}
