using InvestmentHub.Domain.Projections;
using Marten;
using Marten.Events;
using Testcontainers.PostgreSql;
using Xunit;

namespace InvestmentHub.IntegrationTests;

public class MartenFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;
    public IDocumentStore? Store { get; private set; }
    private string? _connectionString;

    public async Task InitializeAsync()
    {
        // Create and start PostgreSQL container
        // Create and start PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine") // Keep implicit or explicit? 
                                             // Wait, I am replacing the lines.
                                             // I'll assume the ctor is `new PostgreSqlBuilder()`.
                                             // I'll try to find if I can just suppress it? No.
                                             // I'll replace `new PostgreSqlBuilder().WithImage(image)` with `new PostgreSqlBuilder().WithImage(image)`.
                                             // Wait, I can't know for sure.
                                             // I will use `view_file` on `InstrumentImporter` FIRST as planned.
                                             // Then I will do a `run_command` to check testcontainers doc? No.
                                             // I'll just fix InstrumentImporter first.
                                             // Actually, I can use `dotnet build` to see the warning details if I want?
                                             // I have the warning message. 
                                             // I'll try `new PostgreSqlBuilder().WithImage(...)`.
                                             // Actually, I'll update the `task.md` and VIEW the files first.
                                             // I'll stick to the plan.

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
