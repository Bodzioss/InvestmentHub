using FluentAssertions;
using InvestmentHub.Domain.Events;
using InvestmentHub.Domain.Projections;
using InvestmentHub.Domain.ReadModels;
using InvestmentHub.Domain.ValueObjects;
using Xunit;

namespace InvestmentHub.Domain.Tests.Projections;

public class PortfolioProjectionTests
{
    private readonly PortfolioProjection _projection;

    public PortfolioProjectionTests()
    {
        _projection = new PortfolioProjection();
    }

    [Fact]
    public void Create_ShouldInitializeReadModelFromPortfolioCreatedEvent()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        var name = "Test Portfolio";
        var description = "Test Description";
        var createdAt = DateTime.UtcNow;

        var @event = new PortfolioCreatedEvent(portfolioId, ownerId, name, description, "USD", createdAt);

        // Act
        var readModel = _projection.Create(@event);

        // Assert
        readModel.Should().NotBeNull();
        readModel.Id.Should().Be(portfolioId.Value);
        readModel.OwnerId.Should().Be(ownerId.Value);
        readModel.Name.Should().Be(name);
        readModel.Description.Should().Be(description);
        readModel.CreatedAt.Should().Be(createdAt);
        readModel.IsClosed.Should().BeFalse();
        readModel.TotalValue.Should().Be(0);
        readModel.Currency.Should().Be("USD");
        readModel.InvestmentCount.Should().Be(0);
        readModel.LastUpdated.Should().Be(@event.OccurredOn);
        readModel.AggregateVersion.Should().Be(@event.Version);
    }

    [Fact]
    public void Create_ShouldHandleNullDescription()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        var name = "Test Portfolio";
        string? description = null;
        var createdAt = DateTime.UtcNow;

        var @event = new PortfolioCreatedEvent(portfolioId, ownerId, name, description, "USD", createdAt);

        // Act
        var readModel = _projection.Create(@event);

        // Assert
        readModel.Description.Should().BeNull();
    }

    [Fact]
    public void Apply_ShouldUpdateNameWhenPortfolioRenamed()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        var oldName = "Old Name";
        var newName = "New Name";
        var createdAt = DateTime.UtcNow;

        var createdEvent = new PortfolioCreatedEvent(portfolioId, ownerId, oldName, "Desc", "USD", createdAt);
        var readModel = _projection.Create(createdEvent);

        var renamedEvent = new PortfolioRenamedEvent(portfolioId, oldName, newName, DateTime.UtcNow.AddMinutes(1));

        // Act
        _projection.Apply(readModel, renamedEvent);

        // Assert
        readModel.Name.Should().Be(newName);
        readModel.LastUpdated.Should().Be(renamedEvent.OccurredOn);
        readModel.AggregateVersion.Should().Be(renamedEvent.Version);
    }

    [Fact]
    public void Apply_ShouldHandleNullReasonWhenPortfolioClosed()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        var name = "Test Portfolio";
        var createdAt = DateTime.UtcNow;

        var createdEvent = new PortfolioCreatedEvent(portfolioId, ownerId, name, "Desc", "USD", createdAt);
        var readModel = _projection.Create(createdEvent);

        var closedBy = UserId.New();
        string? reason = null;
        var closedAt = DateTime.UtcNow.AddDays(1);
        var closedEvent = new PortfolioClosedEvent(portfolioId, name, reason, closedAt, closedBy);

        // Act
        _projection.Apply(readModel, closedEvent);

        // Assert
        readModel.IsClosed.Should().BeTrue();
        readModel.CloseReason.Should().BeNull();
    }

    [Fact]
    public void ProjectionFlow_ShouldHandleFullLifecycleCorrectly()
    {
        // Arrange
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        var initialName = "Initial Portfolio";
        var renamedName = "Renamed Portfolio";
        var createdAt = DateTime.UtcNow;

        // Act - Create
        var createdEvent = new PortfolioCreatedEvent(portfolioId, ownerId, initialName, "Description", "USD", createdAt);
        var readModel = _projection.Create(createdEvent);

        // Act - Rename
        var renamedEvent = new PortfolioRenamedEvent(portfolioId, initialName, renamedName, createdAt.AddMinutes(5));
        _projection.Apply(readModel, renamedEvent);

        // Act - Close
        var closedBy = UserId.New();
        var closedEvent = new PortfolioClosedEvent(portfolioId, renamedName, "Closing", createdAt.AddMinutes(10), closedBy);
        _projection.Apply(readModel, closedEvent);

        // Assert
        readModel.Id.Should().Be(portfolioId.Value);
        readModel.Name.Should().Be(renamedName);
        readModel.IsClosed.Should().BeTrue();
        readModel.ClosedAt.Should().Be(closedEvent.ClosedAt);
        readModel.CloseReason.Should().Be("Closing");
        readModel.AggregateVersion.Should().Be(1); // Each event sets its own version
        readModel.LastUpdated.Should().Be(closedEvent.OccurredOn);
    }

    [Fact]
    public void ProjectionName_ShouldBePortfolio()
    {
        // Assert
        _projection.ProjectionName.Should().Be("Portfolio");
    }
}

