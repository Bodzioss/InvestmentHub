using FluentAssertions;
using InvestmentHub.Domain.Events;
using InvestmentHub.Domain.ValueObjects;
using Xunit;

namespace InvestmentHub.Domain.Tests.Events;

public class PortfolioEventsTests
{
    private readonly PortfolioId _portfolioId = PortfolioId.New();
    private readonly UserId _ownerId = UserId.New();

    [Fact]
    public void PortfolioCreatedEvent_ShouldCreateValidEvent()
    {
        // Arrange
        var name = "My Investment Portfolio";
        var description = "Test portfolio description";
        var createdAt = DateTime.UtcNow;

        // Act
        var evt = new PortfolioCreatedEvent(
            _portfolioId,
            _ownerId,
            name,
            description,
            createdAt);

        // Assert
        evt.Should().NotBeNull();
        evt.PortfolioId.Should().Be(_portfolioId);
        evt.OwnerId.Should().Be(_ownerId);
        evt.Name.Should().Be(name);
        evt.Description.Should().Be(description);
        evt.CreatedAt.Should().Be(createdAt);
        evt.Version.Should().Be(1);
    }

    [Fact]
    public void PortfolioCreatedEvent_ShouldThrowWhenNameIsEmpty()
    {
        // Arrange
        var emptyName = "";

        // Act & Assert
        var act = () => new PortfolioCreatedEvent(
            _portfolioId,
            _ownerId,
            emptyName,
            null,
            DateTime.UtcNow);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*name*");
    }

    [Fact]
    public void PortfolioRenamedEvent_ShouldCreateValidEvent()
    {
        // Arrange
        var oldName = "Old Portfolio Name";
        var newName = "New Portfolio Name";
        var renamedAt = DateTime.UtcNow;

        // Act
        var evt = new PortfolioRenamedEvent(
            _portfolioId,
            oldName,
            newName,
            renamedAt);

        // Assert
        evt.Should().NotBeNull();
        evt.PortfolioId.Should().Be(_portfolioId);
        evt.OldName.Should().Be(oldName);
        evt.NewName.Should().Be(newName);
        evt.RenamedAt.Should().Be(renamedAt);
        evt.Version.Should().Be(1);
    }

    [Fact]
    public void PortfolioRenamedEvent_ShouldThrowWhenNamesAreIdentical()
    {
        // Arrange
        var sameName = "Same Name";

        // Act & Assert
        var act = () => new PortfolioRenamedEvent(
            _portfolioId,
            sameName,
            sameName,
            DateTime.UtcNow);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*different*");
    }

    [Fact]
    public void PortfolioClosedEvent_ShouldCreateValidEvent()
    {
        // Arrange
        var name = "Closed Portfolio";
        var reason = "No longer needed";
        var closedAt = DateTime.UtcNow;
        var closedBy = UserId.New();

        // Act
        var evt = new PortfolioClosedEvent(
            _portfolioId,
            name,
            reason,
            closedAt,
            closedBy);

        // Assert
        evt.Should().NotBeNull();
        evt.PortfolioId.Should().Be(_portfolioId);
        evt.Name.Should().Be(name);
        evt.Reason.Should().Be(reason);
        evt.ClosedAt.Should().Be(closedAt);
        evt.ClosedBy.Should().Be(closedBy);
        evt.Version.Should().Be(1);
    }

    [Fact]
    public void PortfolioClosedEvent_ShouldAcceptNullReason()
    {
        // Arrange & Act
        var evt = new PortfolioClosedEvent(
            _portfolioId,
            "Portfolio Name",
            null, // Reason is optional
            DateTime.UtcNow,
            _ownerId);

        // Assert
        evt.Reason.Should().BeNull();
    }

    [Fact]
    public void PortfolioEvents_ShouldHaveProperToStringRepresentation()
    {
        // Arrange
        var createdEvent = new PortfolioCreatedEvent(
            _portfolioId,
            _ownerId,
            "Test Portfolio",
            null,
            DateTime.UtcNow);

        var renamedEvent = new PortfolioRenamedEvent(
            _portfolioId,
            "Old Name",
            "New Name",
            DateTime.UtcNow);

        var closedEvent = new PortfolioClosedEvent(
            _portfolioId,
            "Test Portfolio",
            "Test reason",
            DateTime.UtcNow,
            _ownerId);

        // Act & Assert
        createdEvent.ToString().Should().Contain("PortfolioCreated");
        createdEvent.ToString().Should().Contain("Test Portfolio");

        renamedEvent.ToString().Should().Contain("PortfolioRenamed");
        renamedEvent.ToString().Should().Contain("Old Name");
        renamedEvent.ToString().Should().Contain("New Name");

        closedEvent.ToString().Should().Contain("PortfolioClosed");
        closedEvent.ToString().Should().Contain("Test reason");
    }
}

