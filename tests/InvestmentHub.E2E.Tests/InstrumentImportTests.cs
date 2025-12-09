using FluentAssertions;
using InvestmentHub.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace InvestmentHub.E2E.Tests;

public class InstrumentImportTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .Build();

    private readonly RabbitMqContainer _rabbitmq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management")
        .Build();

    private WebApplicationFactory<Program> _apiFactory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _rabbitmq.StartAsync();

        _apiFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureServices(services =>
                {
                    services.AddControllers().AddNewtonsoftJson();
                });

                builder.UseSetting("ConnectionStrings:postgres", _postgres.GetConnectionString());
                builder.UseSetting("RabbitMQ:ConnectionString", _rabbitmq.GetConnectionString());
            });
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _rabbitmq.DisposeAsync();
        await _apiFactory.DisposeAsync();
    }

    [Fact]
    public async Task Should_Import_Instruments_On_Startup()
    {
        // Act
        // Create client to trigger application startup and seeding
        _apiFactory.CreateClient();
        
        // Wait for seeding to complete (it happens synchronously in Program.cs before Run)
        // But we need to access the DbContext to verify
        
        using var scope = _apiFactory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Assert
        var count = await context.Instruments.CountAsync();
        count.Should().BeGreaterThan(0, "Instruments should be imported from the JSON file");
        
        // Verify specific instruments
        var apple = await context.Instruments.FirstOrDefaultAsync(i => i.Symbol.Ticker == "AAPL");
        apple.Should().NotBeNull();
        apple!.Name.Should().Be("APPLE");
        apple!.Symbol.Exchange.Should().Be("GLOBALCONNECT"); // Group 30 -> GlobalConnect
        apple.Symbol.AssetType.ToString().Should().Be("Stock");

        var pko = await context.Instruments.FirstOrDefaultAsync(i => i.Symbol.Ticker == "ALR"); // ALIOR is group 01
        pko.Should().NotBeNull();
        pko!.Name.Should().Be("ALIOR");
        pko!.Symbol.Exchange.Should().Be("GPW");
        pko.Symbol.AssetType.ToString().Should().Be("Stock");
        
        var bond = await context.Instruments.FirstOrDefaultAsync(i => i.Symbol.Ticker == "ABE0227"); // Group 70 -> Catalyst
        bond.Should().NotBeNull();
        bond!.Symbol.Exchange.Should().Be("CATALYST");
        bond.Symbol.AssetType.ToString().Should().Be("Bond");
    }
}
