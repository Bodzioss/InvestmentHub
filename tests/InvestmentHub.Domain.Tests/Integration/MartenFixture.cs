using InvestmentHub.Domain.Projections;
using Marten;
using Marten.Events;
using Testcontainers.PostgreSql;
using Xunit;

namespace InvestmentHub.Domain.Tests.Integration;

public class MartenFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;
    public IDocumentStore? Store { get; private set; }
    private string? _connectionString;

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
        Store = DocumentStore.For(options =>
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
        await Store.Advanced.Clean.CompletelyRemoveAllAsync();
    }

    public async Task DisposeAsync()
    {
        Store?.Dispose();
        
        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }
}
