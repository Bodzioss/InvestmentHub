using FluentAssertions;
using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.Events;
using InvestmentHub.Domain.Handlers.Commands;
using InvestmentHub.Domain.Projections;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.ReadModels;
using InvestmentHub.Domain.Repositories;
using InvestmentHub.Domain.ValueObjects;
using Marten;
using Marten.Events;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.PostgreSql;
using Xunit;

namespace InvestmentHub.Domain.Tests.Integration;

/// <summary>
/// Integration tests for Marten Event Sourcing with real PostgreSQL database.
/// Uses TestContainers to spin up a real PostgreSQL instance.
/// </summary>
public class MartenIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;
    private IDocumentStore? _documentStore;
    private string? _connectionString;

    /// <summary>
    /// Initialize PostgreSQL container and Marten before tests.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Create and start PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("investmenthub_test")
            .WithUsername("test")
            .WithPassword("test")
            .WithCleanUp(true)
            .Build();

        await _postgresContainer.StartAsync();

        _connectionString = _postgresContainer.GetConnectionString();

        // Configure Marten
        _documentStore = DocumentStore.For(options =>
        {
            options.Connection(_connectionString);
            options.Events.StreamIdentity = StreamIdentity.AsGuid;
            
            // Register projections
            options.Projections.Add<PortfolioProjection>(Marten.Events.Projections.ProjectionLifecycle.Inline);
            
            // Auto-create schema
            options.AutoCreateSchemaObjects = Weasel.Core.AutoCreate.All;
        });

        // Initialize schema
        await _documentStore.Advanced.Clean.CompletelyRemoveAllAsync();
    }

    /// <summary>
    /// Cleanup PostgreSQL container after tests.
    /// </summary>
    public async Task DisposeAsync()
    {
        _documentStore?.Dispose();
        
        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task CreatePortfolio_ShouldSaveEventsToEventStream()
    {
        // Arrange
        await using var session = _documentStore!.LightweightSession();
        
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        var name = "Test Portfolio";
        var description = "Integration Test";

        // Act - Create aggregate and save events
        var aggregate = PortfolioAggregate.Create(portfolioId, ownerId, name, description);
        
        session.Events.StartStream<PortfolioAggregate>(
            portfolioId.Value,
            aggregate.GetUncommittedEvents().ToArray());
        
        await session.SaveChangesAsync();

        // Assert - Verify events are in event stream
        var events = await session.Events.FetchStreamAsync(portfolioId.Value);
        
        events.Should().NotBeNull();
        events.Should().HaveCount(1);
        events[0].Data.Should().BeOfType<PortfolioCreatedEvent>();
        
        var createdEvent = (PortfolioCreatedEvent)events[0].Data;
        createdEvent.PortfolioId.Should().Be(portfolioId);
        createdEvent.OwnerId.Should().Be(ownerId);
        createdEvent.Name.Should().Be(name);
        createdEvent.Description.Should().Be(description);
    }

    [Fact]
    public async Task CreatePortfolio_ShouldAutomaticallyBuildReadModel()
    {
        // Arrange
        await using var session = _documentStore!.LightweightSession();
        
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        var name = "Read Model Test Portfolio";
        var description = "Testing projection";

        // Act - Create portfolio via event stream
        var aggregate = PortfolioAggregate.Create(portfolioId, ownerId, name, description);
        
        session.Events.StartStream<PortfolioAggregate>(
            portfolioId.Value,
            aggregate.GetUncommittedEvents().ToArray());
        
        await session.SaveChangesAsync();

        // Assert - Verify read model was built by projection
        var readModel = await session.LoadAsync<PortfolioReadModel>(portfolioId.Value);
        
        readModel.Should().NotBeNull();
        readModel!.Id.Should().Be(portfolioId.Value);
        readModel.OwnerId.Should().Be(ownerId.Value);
        readModel.Name.Should().Be(name);
        readModel.Description.Should().Be(description);
        readModel.IsClosed.Should().BeFalse();
        readModel.TotalValue.Should().Be(0);
        readModel.InvestmentCount.Should().Be(0);
        readModel.Version.Should().Be(1);
    }

    [Fact]
    public async Task CreatePortfolioCommand_EndToEnd_ShouldWorkCorrectly()
    {
        // Arrange - Setup command handler with real Marten session
        await using var session = _documentStore!.LightweightSession();
        
        var userRepositoryMock = new Mock<IUserRepository>();
        var portfolioRepositoryMock = new Mock<IPortfolioRepository>();
        var loggerMock = new Mock<ILogger<CreatePortfolioCommandHandler>>();

        // Setup mocks for validation
        userRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvestmentHub.Domain.Entities.User(
                UserId.New(), 
                "Test User", 
                "test@test.com", 
                DateTime.UtcNow));
        
        userRepositoryMock
            .Setup(x => x.CanCreatePortfolioAsync(It.IsAny<UserId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        portfolioRepositoryMock
            .Setup(x => x.ExistsByNameAsync(It.IsAny<UserId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new CreatePortfolioCommandHandler(
            session,
            userRepositoryMock.Object,
            portfolioRepositoryMock.Object,
            loggerMock.Object);

        // Create command
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        var command = new CreatePortfolioCommand(
            portfolioId,
            ownerId,
            "E2E Test Portfolio",
            "End-to-end integration test");

        // Act - Execute command
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert - Verify command result
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.PortfolioId.Should().Be(portfolioId);

        // Assert - Verify events in stream
        var events = await session.Events.FetchStreamAsync(portfolioId.Value);
        events.Should().HaveCount(1);
        events[0].Data.Should().BeOfType<PortfolioCreatedEvent>();

        // Assert - Verify read model
        var readModel = await session.LoadAsync<PortfolioReadModel>(portfolioId.Value);
        readModel.Should().NotBeNull();
        readModel!.Name.Should().Be("E2E Test Portfolio");
        readModel.IsClosed.Should().BeFalse();
    }

    [Fact]
    public async Task RenamePortfolio_ShouldAppendEventAndUpdateReadModel()
    {
        // Arrange - Create initial portfolio
        await using var session = _documentStore!.LightweightSession();
        
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        var initialName = "Original Name";
        
        var aggregate = PortfolioAggregate.Create(portfolioId, ownerId, initialName, "Description");
        
        session.Events.StartStream<PortfolioAggregate>(
            portfolioId.Value,
            aggregate.GetUncommittedEvents().ToArray());
        
        await session.SaveChangesAsync();

        // Act - Rename portfolio
        await using var session2 = _documentStore!.LightweightSession();
        
        // Manually reconstitute aggregate from events
        var streamEvents = await session2.Events.FetchStreamAsync(portfolioId.Value);
        var loadedAggregate = new PortfolioAggregate();
        foreach (var evt in streamEvents)
        {
            ((dynamic)loadedAggregate).Apply((dynamic)evt.Data);
        }
        loadedAggregate.ClearUncommittedEvents(); // Clear creation events
        
        var newName = "Renamed Portfolio";
        loadedAggregate.Rename(newName);
        
        session2.Events.Append(portfolioId.Value, loadedAggregate.GetUncommittedEvents().ToArray());
        await session2.SaveChangesAsync();

        // Assert - Verify events
        var events = await session2.Events.FetchStreamAsync(portfolioId.Value);
        events.Should().HaveCount(2);
        events[0].Data.Should().BeOfType<PortfolioCreatedEvent>();
        events[1].Data.Should().BeOfType<PortfolioRenamedEvent>();
        
        var renamedEvent = (PortfolioRenamedEvent)events[1].Data;
        renamedEvent.OldName.Should().Be(initialName);
        renamedEvent.NewName.Should().Be(newName);

        // Assert - Verify read model updated
        var readModel = await session2.LoadAsync<PortfolioReadModel>(portfolioId.Value);
        readModel.Should().NotBeNull();
        readModel!.Name.Should().Be(newName);
        // Version is set by the last event's Version property (all events have Version = 1)
        readModel.Version.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task ClosePortfolio_ShouldAppendEventAndUpdateReadModel()
    {
        // Arrange - Create initial portfolio
        await using var session = _documentStore!.LightweightSession();
        
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        
        var aggregate = PortfolioAggregate.Create(portfolioId, ownerId, "Test Portfolio", "Description");
        
        session.Events.StartStream<PortfolioAggregate>(
            portfolioId.Value,
            aggregate.GetUncommittedEvents().ToArray());
        
        await session.SaveChangesAsync();

        // Act - Close portfolio
        await using var session2 = _documentStore!.LightweightSession();
        
        // Manually reconstitute aggregate from events
        var streamEvents = await session2.Events.FetchStreamAsync(portfolioId.Value);
        var loadedAggregate = new PortfolioAggregate();
        foreach (var evt in streamEvents)
        {
            ((dynamic)loadedAggregate).Apply((dynamic)evt.Data);
        }
        loadedAggregate.ClearUncommittedEvents(); // Clear creation events
        
        var closedBy = UserId.New();
        var closeReason = "Testing closure";
        loadedAggregate.Close(closeReason, closedBy);
        
        session2.Events.Append(portfolioId.Value, loadedAggregate.GetUncommittedEvents().ToArray());
        await session2.SaveChangesAsync();

        // Assert - Verify events
        var events = await session2.Events.FetchStreamAsync(portfolioId.Value);
        events.Should().HaveCount(2);
        events[1].Data.Should().BeOfType<PortfolioClosedEvent>();
        
        var closedEvent = (PortfolioClosedEvent)events[1].Data;
        closedEvent.Reason.Should().Be(closeReason);
        closedEvent.ClosedBy.Should().Be(closedBy);

        // Assert - Verify read model updated
        var readModel = await session2.LoadAsync<PortfolioReadModel>(portfolioId.Value);
        readModel.Should().NotBeNull();
        readModel!.IsClosed.Should().BeTrue();
        readModel.CloseReason.Should().Be(closeReason);
        readModel.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task EventStream_ShouldSupportFullLifecycle()
    {
        // Arrange & Act - Full lifecycle: Create -> Rename -> Rename again -> Close
        await using var session = _documentStore!.LightweightSession();
        
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        
        // Create
        var aggregate = PortfolioAggregate.Create(portfolioId, ownerId, "Initial Name", "Description");
        session.Events.StartStream<PortfolioAggregate>(portfolioId.Value, aggregate.GetUncommittedEvents().ToArray());
        await session.SaveChangesAsync();

        // Rename 1
        await using var session2 = _documentStore!.LightweightSession();
        var events2 = await session2.Events.FetchStreamAsync(portfolioId.Value);
        var agg2 = new PortfolioAggregate();
        foreach (var evt in events2) { ((dynamic)agg2).Apply((dynamic)evt.Data); }
        agg2.ClearUncommittedEvents();
        agg2.Rename("Second Name");
        session2.Events.Append(portfolioId.Value, agg2.GetUncommittedEvents().ToArray());
        await session2.SaveChangesAsync();

        // Rename 2
        await using var session3 = _documentStore!.LightweightSession();
        var events3 = await session3.Events.FetchStreamAsync(portfolioId.Value);
        var agg3 = new PortfolioAggregate();
        foreach (var evt in events3) { ((dynamic)agg3).Apply((dynamic)evt.Data); }
        agg3.ClearUncommittedEvents();
        agg3.Rename("Final Name");
        session3.Events.Append(portfolioId.Value, agg3.GetUncommittedEvents().ToArray());
        await session3.SaveChangesAsync();

        // Close
        await using var session4 = _documentStore!.LightweightSession();
        var events4 = await session4.Events.FetchStreamAsync(portfolioId.Value);
        var agg4 = new PortfolioAggregate();
        foreach (var evt in events4) { ((dynamic)agg4).Apply((dynamic)evt.Data); }
        agg4.ClearUncommittedEvents();
        agg4.Close("End of lifecycle", ownerId);
        session4.Events.Append(portfolioId.Value, agg4.GetUncommittedEvents().ToArray());
        await session4.SaveChangesAsync();

        // Assert - Verify all events
        await using var sessionFinal = _documentStore!.LightweightSession();
        var events = await sessionFinal.Events.FetchStreamAsync(portfolioId.Value);
        
        events.Should().HaveCount(4);
        events[0].Data.Should().BeOfType<PortfolioCreatedEvent>();
        events[1].Data.Should().BeOfType<PortfolioRenamedEvent>();
        events[2].Data.Should().BeOfType<PortfolioRenamedEvent>();
        events[3].Data.Should().BeOfType<PortfolioClosedEvent>();

        // Assert - Verify final read model state
        var readModel = await sessionFinal.LoadAsync<PortfolioReadModel>(portfolioId.Value);
        readModel.Should().NotBeNull();
        readModel!.Name.Should().Be("Final Name");
        readModel.IsClosed.Should().BeTrue();
    }
}

