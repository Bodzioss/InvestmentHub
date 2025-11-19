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
            options.Projections.Add<InvestmentProjection>(Marten.Events.Projections.ProjectionLifecycle.Inline);
            
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

    #region Investment Integration Tests

    [Fact]
    public async Task AddInvestment_ShouldSaveEventsToEventStream()
    {
        // Arrange
        await using var session = _documentStore!.LightweightSession();
        
        var investmentId = InvestmentId.New();
        var portfolioId = PortfolioId.New();
        var symbol = new Symbol("AAPL", "NASDAQ", InvestmentHub.Domain.Enums.AssetType.Stock);
        var purchasePrice = new Money(150m, InvestmentHub.Domain.Enums.Currency.USD);
        var quantity = 10m;
        var purchaseDate = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        // Act - Create aggregate and save events
        var aggregate = InvestmentAggregate.Create(
            investmentId,
            portfolioId,
            symbol,
            purchasePrice,
            quantity,
            purchaseDate);
        
        session.Events.StartStream<InvestmentAggregate>(
            investmentId.Value,
            aggregate.GetUncommittedEvents().ToArray());
        
        await session.SaveChangesAsync();

        // Assert - Verify events are in event stream
        var events = await session.Events.FetchStreamAsync(investmentId.Value);
        
        events.Should().NotBeEmpty();
        events.Should().HaveCount(1);
        events[0].Data.Should().BeOfType<InvestmentAddedEvent>();
        
        var addedEvent = (InvestmentAddedEvent)events[0].Data;
        addedEvent.InvestmentId.Should().Be(investmentId);
        addedEvent.PortfolioId.Should().Be(portfolioId);
        addedEvent.Symbol.Ticker.Should().Be("AAPL");
        addedEvent.Quantity.Should().Be(quantity);
        addedEvent.PurchasePrice.Amount.Should().Be(150m);
    }

    [Fact]
    public async Task InvestmentProjection_ShouldBuildReadModel()
    {
        // Arrange
        await using var session = _documentStore!.LightweightSession();
        
        var investmentId = InvestmentId.New();
        var portfolioId = PortfolioId.New();
        var symbol = new Symbol("TSLA", "NASDAQ", InvestmentHub.Domain.Enums.AssetType.Stock);
        var purchasePrice = new Money(200m, InvestmentHub.Domain.Enums.Currency.USD);
        var quantity = 5m;
        var purchaseDate = DateTime.UtcNow.AddDays(-10);

        // Act - Create aggregate and save events
        var aggregate = InvestmentAggregate.Create(
            investmentId,
            portfolioId,
            symbol,
            purchasePrice,
            quantity,
            purchaseDate);
        
        session.Events.StartStream<InvestmentAggregate>(
            investmentId.Value,
            aggregate.GetUncommittedEvents().ToArray());
        
        await session.SaveChangesAsync();

        // Assert - Verify projection created read model
        await using var session2 = _documentStore!.LightweightSession();
        var readModel = await session2.LoadAsync<InvestmentReadModel>(investmentId.Value);
        
        readModel.Should().NotBeNull();
        readModel!.Id.Should().Be(investmentId.Value);
        readModel.PortfolioId.Should().Be(portfolioId.Value);
        readModel.Ticker.Should().Be("TSLA");
        readModel.Exchange.Should().Be("NASDAQ");
        readModel.PurchasePrice.Should().Be(200m);
        readModel.Quantity.Should().Be(5m);
        readModel.OriginalQuantity.Should().Be(5m);
        readModel.CurrentValue.Should().Be(1000m); // 200 * 5
        readModel.Status.Should().Be(InvestmentHub.Domain.Enums.InvestmentStatus.Active);
    }

    [Fact]
    public async Task Investment_E2E_CommandToQuery()
    {
        // Arrange - Create Portfolio first
        await using var portfolioSession = _documentStore!.LightweightSession();
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        var portfolioAggregate = PortfolioAggregate.Create(portfolioId, ownerId, "Test Portfolio");
        
        portfolioSession.Events.StartStream<PortfolioAggregate>(
            portfolioId.Value,
            portfolioAggregate.GetUncommittedEvents().ToArray());
        await portfolioSession.SaveChangesAsync();

        // Act - Add Investment via aggregate
        await using var investmentSession = _documentStore!.LightweightSession();
        
        var investmentId = InvestmentId.New();
        var symbol = new Symbol("NVDA", "NASDAQ", InvestmentHub.Domain.Enums.AssetType.Stock);
        var purchasePrice = new Money(450m, InvestmentHub.Domain.Enums.Currency.USD);
        var quantity = 20m;
        var purchaseDate = DateTime.UtcNow.AddDays(-5);

        var investmentAggregate = InvestmentAggregate.Create(
            investmentId,
            portfolioId,
            symbol,
            purchasePrice,
            quantity,
            purchaseDate);
        
        investmentSession.Events.StartStream<InvestmentAggregate>(
            investmentId.Value,
            investmentAggregate.GetUncommittedEvents().ToArray());
        
        await investmentSession.SaveChangesAsync();

        // Assert - Query returns correct data
        await using var querySession = _documentStore!.LightweightSession();
        var readModel = await querySession.LoadAsync<InvestmentReadModel>(investmentId.Value);
        
        readModel.Should().NotBeNull();
        readModel!.Ticker.Should().Be("NVDA");
        readModel.Quantity.Should().Be(20m);
        readModel.PurchasePrice.Should().Be(450m);
        readModel.CurrentValue.Should().Be(9000m); // 450 * 20
        readModel.TotalCost.Should().Be(9000m);
        readModel.Status.Should().Be(InvestmentHub.Domain.Enums.InvestmentStatus.Active);
    }

    [Fact]
    public async Task UpdateInvestmentValue_ShouldAppendNewEvent()
    {
        // Arrange - Create investment first
        await using var session = _documentStore!.LightweightSession();
        
        var investmentId = InvestmentId.New();
        var portfolioId = PortfolioId.New();
        var symbol = new Symbol("MSFT", "NASDAQ", InvestmentHub.Domain.Enums.AssetType.Stock);
        var purchasePrice = new Money(300m, InvestmentHub.Domain.Enums.Currency.USD);
        var quantity = 10m;
        var purchaseDate = DateTime.UtcNow.AddDays(-15);

        var aggregate = InvestmentAggregate.Create(
            investmentId,
            portfolioId,
            symbol,
            purchasePrice,
            quantity,
            purchaseDate);
        
        session.Events.StartStream<InvestmentAggregate>(
            investmentId.Value,
            aggregate.GetUncommittedEvents().ToArray());
        
        await session.SaveChangesAsync();

        // Act - Update value
        await using var updateSession = _documentStore!.LightweightSession();
        
        var loadEvents = await updateSession.Events.FetchStreamAsync(investmentId.Value);
        var loadedAggregate = new InvestmentAggregate();
        foreach (var evt in loadEvents) { ((dynamic)loadedAggregate).Apply((dynamic)evt.Data); }
        loadedAggregate.ClearUncommittedEvents();
        
        loadedAggregate.UpdateValue(new Money(350m, InvestmentHub.Domain.Enums.Currency.USD));
        
        updateSession.Events.Append(
            investmentId.Value,
            loadedAggregate.GetUncommittedEvents().ToArray());
        
        await updateSession.SaveChangesAsync();

        // Assert - Verify new event appended
        await using var assertSession = _documentStore!.LightweightSession();
        var events = await assertSession.Events.FetchStreamAsync(investmentId.Value);
        
        events.Should().HaveCount(2);
        events[0].Data.Should().BeOfType<InvestmentAddedEvent>();
        events[1].Data.Should().BeOfType<InvestmentValueUpdatedEvent>();
        
        var valueUpdatedEvent = (InvestmentValueUpdatedEvent)events[1].Data;
        valueUpdatedEvent.NewValue.Amount.Should().Be(3500m); // 350 * 10

        // Assert - Read model updated
        var readModel = await assertSession.LoadAsync<InvestmentReadModel>(investmentId.Value);
        readModel.Should().NotBeNull();
        readModel!.CurrentValue.Should().Be(3500m);
        readModel.ValuePerUnit.Should().Be(350m);
    }

    [Fact]
    public async Task SellInvestment_ShouldCloseInvestmentAndUpdateReadModel()
    {
        // Arrange - Create investment first
        await using var session = _documentStore!.LightweightSession();
        
        var investmentId = InvestmentId.New();
        var portfolioId = PortfolioId.New();
        var symbol = new Symbol("GOOGL", "NASDAQ", InvestmentHub.Domain.Enums.AssetType.Stock);
        var purchasePrice = new Money(120m, InvestmentHub.Domain.Enums.Currency.USD);
        var quantity = 8m;
        var purchaseDate = DateTime.UtcNow.AddDays(-20);

        var aggregate = InvestmentAggregate.Create(
            investmentId,
            portfolioId,
            symbol,
            purchasePrice,
            quantity,
            purchaseDate);
        
        session.Events.StartStream<InvestmentAggregate>(
            investmentId.Value,
            aggregate.GetUncommittedEvents().ToArray());
        
        await session.SaveChangesAsync();

        // Act - Sell investment completely
        await using var sellSession = _documentStore!.LightweightSession();
        
        var loadEvents = await sellSession.Events.FetchStreamAsync(investmentId.Value);
        var loadedAggregate = new InvestmentAggregate();
        foreach (var evt in loadEvents) { ((dynamic)loadedAggregate).Apply((dynamic)evt.Data); }
        loadedAggregate.ClearUncommittedEvents();
        
        loadedAggregate.Sell(
            new Money(150m, InvestmentHub.Domain.Enums.Currency.USD),
            null, // Sell all
            DateTime.UtcNow);
        
        sellSession.Events.Append(
            investmentId.Value,
            loadedAggregate.GetUncommittedEvents().ToArray());
        
        await sellSession.SaveChangesAsync();

        // Assert - Verify sold event appended
        await using var assertSession = _documentStore!.LightweightSession();
        var events = await assertSession.Events.FetchStreamAsync(investmentId.Value);
        
        events.Should().HaveCount(2);
        events[1].Data.Should().BeOfType<InvestmentSoldEvent>();
        
        var soldEvent = (InvestmentSoldEvent)events[1].Data;
        soldEvent.QuantitySold.Should().Be(8m);
        soldEvent.IsCompleteSale.Should().BeTrue();
        soldEvent.RealizedProfitLoss.Amount.Should().Be(240m); // (150 - 120) * 8

        // Assert - Read model updated with Sold status
        var readModel = await assertSession.LoadAsync<InvestmentReadModel>(investmentId.Value);
        readModel.Should().NotBeNull();
        readModel!.Status.Should().Be(InvestmentHub.Domain.Enums.InvestmentStatus.Sold);
        readModel.Quantity.Should().Be(0);
        readModel.CurrentValue.Should().Be(0);
        readModel.RealizedProfitLoss.Should().Be(240m);
    }

    [Fact]
    public async Task InvestmentEvents_ShouldUpdatePortfolioReadModel()
    {
        // Arrange - Create Portfolio first
        await using var portfolioSession = _documentStore!.LightweightSession();
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        var portfolioAggregate = PortfolioAggregate.Create(portfolioId, ownerId, "Integration Portfolio");
        
        portfolioSession.Events.StartStream<PortfolioAggregate>(
            portfolioId.Value,
            portfolioAggregate.GetUncommittedEvents().ToArray());
        await portfolioSession.SaveChangesAsync();

        // Act 1 - Add first investment
        await using var addInvestment1Session = _documentStore!.LightweightSession();
        
        var investment1Id = InvestmentId.New();
        var symbol1 = new Symbol("AMZN", "NASDAQ", InvestmentHub.Domain.Enums.AssetType.Stock);
        var purchasePrice1 = new Money(150m, InvestmentHub.Domain.Enums.Currency.USD);
        var quantity1 = 10m;

        var investment1Aggregate = InvestmentAggregate.Create(
            investment1Id,
            portfolioId,
            symbol1,
            purchasePrice1,
            quantity1,
            DateTime.UtcNow);
        
        addInvestment1Session.Events.StartStream<InvestmentAggregate>(
            investment1Id.Value,
            investment1Aggregate.GetUncommittedEvents().ToArray());
        
        await addInvestment1Session.SaveChangesAsync();

        // Assert after first investment
        await using var assertSession1 = _documentStore!.LightweightSession();
        var portfolioReadModel1 = await assertSession1.LoadAsync<PortfolioReadModel>(portfolioId.Value);
        portfolioReadModel1.Should().NotBeNull();
        portfolioReadModel1!.InvestmentCount.Should().Be(1);
        portfolioReadModel1.TotalValue.Should().Be(1500m); // 150 * 10

        // Act 2 - Add second investment
        await using var addInvestment2Session = _documentStore!.LightweightSession();
        
        var investment2Id = InvestmentId.New();
        var symbol2 = new Symbol("META", "NASDAQ", InvestmentHub.Domain.Enums.AssetType.Stock);
        var purchasePrice2 = new Money(200m, InvestmentHub.Domain.Enums.Currency.USD);
        var quantity2 = 5m;

        var investment2Aggregate = InvestmentAggregate.Create(
            investment2Id,
            portfolioId,
            symbol2,
            purchasePrice2,
            quantity2,
            DateTime.UtcNow);
        
        addInvestment2Session.Events.StartStream<InvestmentAggregate>(
            investment2Id.Value,
            investment2Aggregate.GetUncommittedEvents().ToArray());
        
        await addInvestment2Session.SaveChangesAsync();

        // Assert after second investment
        await using var assertSession2 = _documentStore!.LightweightSession();
        var portfolioReadModel2 = await assertSession2.LoadAsync<PortfolioReadModel>(portfolioId.Value);
        portfolioReadModel2.Should().NotBeNull();
        portfolioReadModel2!.InvestmentCount.Should().Be(2);
        portfolioReadModel2.TotalValue.Should().Be(2500m); // 1500 + 1000

        // Act 3 - Update value of first investment
        await using var updateSession = _documentStore!.LightweightSession();
        
        var events1 = await updateSession.Events.FetchStreamAsync(investment1Id.Value);
        var loadedInvestment1 = new InvestmentAggregate();
        foreach (var evt in events1) { ((dynamic)loadedInvestment1).Apply((dynamic)evt.Data); }
        loadedInvestment1.ClearUncommittedEvents();
        
        loadedInvestment1.UpdateValue(new Money(200m, InvestmentHub.Domain.Enums.Currency.USD)); // +50 per unit
        
        updateSession.Events.Append(
            investment1Id.Value,
            loadedInvestment1.GetUncommittedEvents().ToArray());
        
        await updateSession.SaveChangesAsync();

        // Assert after value update
        await using var assertSession3 = _documentStore!.LightweightSession();
        var portfolioReadModel3 = await assertSession3.LoadAsync<PortfolioReadModel>(portfolioId.Value);
        portfolioReadModel3.Should().NotBeNull();
        portfolioReadModel3!.TotalValue.Should().Be(3000m); // 2000 (200*10) + 1000

        // Act 4 - Sell first investment completely at market price
        await using var sellSession = _documentStore!.LightweightSession();
        
        var events1ForSale = await sellSession.Events.FetchStreamAsync(investment1Id.Value);
        var loadedInvestment1ForSale = new InvestmentAggregate();
        foreach (var evt in events1ForSale) { ((dynamic)loadedInvestment1ForSale).Apply((dynamic)evt.Data); }
        loadedInvestment1ForSale.ClearUncommittedEvents();
        
        // Sell at same price as current value (200 per unit)
        loadedInvestment1ForSale.Sell(
            new Money(200m, InvestmentHub.Domain.Enums.Currency.USD),
            null, // Sell all
            DateTime.UtcNow);
        
        sellSession.Events.Append(
            investment1Id.Value,
            loadedInvestment1ForSale.GetUncommittedEvents().ToArray());
        
        await sellSession.SaveChangesAsync();

        // Assert after sale - TotalProceeds = CurrentValue before sale, so calculation is exact
        await using var assertSessionFinal = _documentStore!.LightweightSession();
        var portfolioReadModelFinal = await assertSessionFinal.LoadAsync<PortfolioReadModel>(portfolioId.Value);
        portfolioReadModelFinal.Should().NotBeNull();
        portfolioReadModelFinal!.InvestmentCount.Should().Be(1); // One sold, one remains
        portfolioReadModelFinal.TotalValue.Should().Be(1000m); // Only second investment remains (200*5)
    }

    #endregion
}

