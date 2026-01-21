using FluentAssertions;
using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.Events;
using InvestmentHub.Domain.ValueObjects;
using Xunit;

namespace InvestmentHub.Domain.Tests.Aggregates;

public class PortfolioAggregateTests
{
    private readonly PortfolioId _portfolioId = PortfolioId.New();
    private readonly UserId _ownerId = UserId.New();

    [Fact]
    public void Create_ShouldGeneratePortfolioCreatedEvent_AndSetInitialState()
    {
        // Arrange
        var name = "My Investment Portfolio";
        var description = "Test portfolio";

        // Act
        var aggregate = PortfolioAggregate.Initiate(_portfolioId, _ownerId, name, description);

        // Assert
        aggregate.Should().NotBeNull();
        aggregate.Id.Should().Be(_portfolioId.Value);
        aggregate.PortfolioId.Should().Be(_portfolioId);
        aggregate.OwnerId.Should().Be(_ownerId);
        aggregate.Name.Should().Be(name);
        aggregate.Description.Should().Be(description);
        aggregate.IsClosed.Should().BeFalse();
        aggregate.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        aggregate.Version.Should().Be(1);
    }

    [Fact]
    public void Create_ShouldThrowException_WhenNameIsEmpty()
    {
        // Act & Assert
        var act = () => PortfolioAggregate.Initiate(_portfolioId, _ownerId, "");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*name*");
    }

    [Fact]
    public void Rename_ShouldGeneratePortfolioRenamedEvent_AndUpdateName()
    {
        // Arrange
        var aggregate = PortfolioAggregate.Initiate(_portfolioId, _ownerId, "Old Name");
        var newName = "New Name";

        // Act
        var @event = aggregate.Rename(newName);

        // Assert
        @event.Should().NotBeNull();
        @event.Should().BeOfType<PortfolioRenamedEvent>();
        @event.OldName.Should().Be("Old Name");
        @event.NewName.Should().Be(newName);
        aggregate.Name.Should().Be(newName);
        aggregate.Version.Should().Be(2); // Create = 1, Rename = 2
    }

    [Fact]
    public void Rename_ShouldThrowException_WhenNameIsTheSame()
    {
        // Arrange
        var sameName = "Same Name";
        var aggregate = PortfolioAggregate.Initiate(_portfolioId, _ownerId, sameName);

        // Act & Assert
        var act = () => aggregate.Rename(sameName);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*different*");
    }

    [Fact]
    public void Rename_ShouldThrowException_WhenPortfolioIsClosed()
    {
        // Arrange
        var aggregate = PortfolioAggregate.Initiate(_portfolioId, _ownerId, "Portfolio");
        aggregate.Close("Closing for test", _ownerId);

        // Act & Assert
        var act = () => aggregate.Rename("New Name");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*closed*");
    }

    [Fact]
    public void Close_ShouldGeneratePortfolioClosedEvent_AndMarkAsClosed()
    {
        // Arrange
        var aggregate = PortfolioAggregate.Initiate(_portfolioId, _ownerId, "Portfolio");
        var reason = "No longer needed";

        // Act
        var @event = aggregate.Close(reason, _ownerId);

        // Assert
        @event.Should().NotBeNull();
        @event.Should().BeOfType<PortfolioClosedEvent>();
        @event.Reason.Should().Be(reason);
        aggregate.IsClosed.Should().BeTrue();
        aggregate.ClosedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        aggregate.CloseReason.Should().Be(reason);
        aggregate.Version.Should().Be(2); // Create = 1, Close = 2
    }

    [Fact]
    public void Close_ShouldAcceptNullReason()
    {
        // Arrange
        var aggregate = PortfolioAggregate.Initiate(_portfolioId, _ownerId, "Portfolio");

        // Act
        var @event = aggregate.Close(null, _ownerId);

        // Assert
        @event.Reason.Should().BeNull();
        aggregate.CloseReason.Should().BeNull();
        aggregate.IsClosed.Should().BeTrue();
    }

    [Fact]
    public void Close_ShouldThrowException_WhenAlreadyClosed()
    {
        // Arrange
        var aggregate = PortfolioAggregate.Initiate(_portfolioId, _ownerId, "Portfolio");
        aggregate.Close("First close", _ownerId);

        // Act & Assert
        var act = () => aggregate.Close("Second close", _ownerId);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already closed*");
    }

    [Fact]
    public void EventSourcing_ShouldReplayEventsToRebuildState()
    {
        // Arrange - Create a sequence of events
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        
        var createdEvent = new PortfolioCreatedEvent(
            portfolioId,
            ownerId,
            "Initial Name",
            "Initial Description",
            "USD",
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var renamedEvent = new PortfolioRenamedEvent(
            portfolioId,
            "Initial Name",
            "Updated Name",
            new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        var closedEvent = new PortfolioClosedEvent(
            portfolioId,
            "Updated Name",
            "Portfolio completed",
            new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            ownerId);

        // Act - Replay events to rebuild state
        var aggregate = new PortfolioAggregate();
        aggregate.Apply(createdEvent);
        aggregate.Apply(renamedEvent);
        aggregate.Apply(closedEvent);

        // Assert - Verify final state
        aggregate.Id.Should().Be(portfolioId.Value);
        aggregate.Name.Should().Be("Updated Name");
        aggregate.Description.Should().Be("Initial Description");
        aggregate.IsClosed.Should().BeTrue();
        aggregate.CreatedAt.Should().Be(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        aggregate.ClosedAt.Should().Be(new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        aggregate.CloseReason.Should().Be("Portfolio completed");
        aggregate.Version.Should().Be(3); // 3 events applied
    }

    [Fact]
    public void EventSourcing_ShouldHandleMultipleRenames()
    {
        // Arrange
        var aggregate = PortfolioAggregate.Initiate(_portfolioId, _ownerId, "Name 1");

        // Act
        aggregate.Rename("Name 2");
        aggregate.Rename("Name 3");
        aggregate.Rename("Name 4");

        // Assert
        aggregate.Name.Should().Be("Name 4");
        aggregate.Version.Should().Be(4); // 1 Create + 3 Renames
    }

    [Fact]
    public void ToString_ShouldReturnMeaningfulRepresentation()
    {
        // Arrange
        var aggregate = PortfolioAggregate.Initiate(_portfolioId, _ownerId, "Test Portfolio");

        // Act
        var result = aggregate.ToString();

        // Assert
        result.Should().Contain("Test Portfolio");
        result.Should().Contain(_portfolioId.Value.ToString());
        result.Should().Contain("Closed: False");
        result.Should().Contain("Version: 1");
    }
}

