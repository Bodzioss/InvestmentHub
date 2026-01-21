using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.ValueObjects;
using FluentAssertions;
using Xunit;
using System.ComponentModel.DataAnnotations;

namespace InvestmentHub.Domain.Tests.Commands;

/// <summary>
/// Unit tests for CreatePortfolioCommand.
/// Tests the command structure and validation attributes.
/// </summary>
public class CreatePortfolioCommandTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateCommand()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        var name = "My Investment Portfolio";
        var description = "A diversified investment portfolio";

        // Act
        var command = new CreatePortfolioCommand(
            portfolioId,
            ownerId,
            name,
            description);

        // Assert
        command.Should().NotBeNull();
        command.PortfolioId.Should().Be(portfolioId);
        command.OwnerId.Should().Be(ownerId);
        command.Name.Should().Be(name);
        command.Description.Should().Be(description);
    }

    [Fact]
    public void Constructor_WithNullDescription_ShouldCreateCommand()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        var name = "My Investment Portfolio";
        string? description = null;

        // Act
        var command = new CreatePortfolioCommand(
            portfolioId,
            ownerId,
            name,
            description);

        // Assert
        command.Should().NotBeNull();
        command.Description.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithEmptyDescription_ShouldCreateCommand()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        var name = "My Investment Portfolio";
        var description = "";

        // Act
        var command = new CreatePortfolioCommand(
            portfolioId,
            ownerId,
            name,
            description);

        // Assert
        command.Should().NotBeNull();
        command.Description.Should().Be("");
    }

    [Fact]
    public void Constructor_WithLongName_ShouldCreateCommand()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        var name = "This is a very long portfolio name that should still be valid";
        var description = "A diversified investment portfolio";

        // Act
        var command = new CreatePortfolioCommand(
            portfolioId,
            ownerId,
            name,
            description);

        // Assert
        command.Should().NotBeNull();
        command.Name.Should().Be(name);
    }

    [Fact]
    public void Validation_WithValidData_ShouldPass()
    {
        // Arrange
        var command = new CreatePortfolioCommand(
            PortfolioId.New(),
            UserId.New(),
            "My Investment Portfolio",
            "A diversified investment portfolio");

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(command);

        // Act
        var isValid = Validator.TryValidateObject(command, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void Validation_WithEmptyName_ShouldFail()
    {
        // Arrange
        var command = new CreatePortfolioCommand(
            PortfolioId.New(),
            UserId.New(),
            "", // Invalid name
            "A diversified investment portfolio");

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(command);

        // Act
        var isValid = Validator.TryValidateObject(command, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle();
        validationResults[0].ErrorMessage.Should().Contain("Portfolio name must be between 1 and 100 characters");
    }

    [Fact]
    public void Validation_WithWhitespaceName_ShouldFail()
    {
        // Arrange
        var command = new CreatePortfolioCommand(
            PortfolioId.New(),
            UserId.New(),
            "   ", // Invalid name
            "A diversified investment portfolio");

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(command);

        // Act
        var isValid = Validator.TryValidateObject(command, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle();
        validationResults[0].ErrorMessage.Should().Contain("Portfolio name cannot be empty or only whitespace");
    }
}
