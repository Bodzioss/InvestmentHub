using FluentAssertions;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.ValueObjects;
using Xunit;

namespace InvestmentHub.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void UpdateName_Should_Update_User_Name_Successfully()
    {
        // Arrange
        var userId = new UserId(Guid.NewGuid());
        var originalName = "Original Name";
        var user = new User(userId, originalName, "test@example.com", DateTime.UtcNow);
        var newName = "Updated Name";

        // Act
        user.UpdateName(newName);

        // Assert
        user.Name.Should().Be(newName);
    }

    [Fact]
    public void UpdateName_Should_Throw_When_Name_Is_Null()
    {
        // Arrange
        var userId = new UserId(Guid.NewGuid());
        var user = new User(userId, "Test User", "test@example.com", DateTime.UtcNow);

        // Act & Assert
        var act = () => user.UpdateName(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_Should_Create_User_With_Valid_Parameters()
    {
        // Arrange
        var userId = new UserId(Guid.NewGuid());
        var name = "Test User";
        var email = "test@example.com";
        var createdAt = DateTime.UtcNow;

        // Act
        var user = new User(userId, name, email, createdAt);

        // Assert
        user.Id.Should().Be(userId);
        user.Name.Should().Be(name);
        user.Email.Should().Be(email);
        user.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Id_Is_Null()
    {
        // Act & Assert
        var act = () => new User(null!, "Test", "test@example.com", DateTime.UtcNow);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_Should_Throw_When_Name_Is_Null()
    {
        // Arrange
        var userId = new UserId(Guid.NewGuid());

        // Act & Assert
        var act = () => new User(userId, null!, "test@example.com", DateTime.UtcNow);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_Should_Throw_When_Email_Is_Null()
    {
        // Arrange
        var userId = new UserId(Guid.NewGuid());

        // Act & Assert
        var act = () => new User(userId, "Test", null!, DateTime.UtcNow);
        act.Should().Throw<ArgumentNullException>();
    }
}
