using FluentAssertions;
using InvestmentHub.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace InvestmentHub.E2E.Tests;

public class InstrumentsApiTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .Build();

    private readonly RabbitMqContainer _rabbitmq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management")
        .Build();

    private WebApplicationFactory<Program> _apiFactory;
    private HttpClient _client;

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

        _client = _apiFactory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _rabbitmq.DisposeAsync();
        await _apiFactory.DisposeAsync();
    }

    [Fact]
    public async Task Should_Search_And_Filter_Instruments()
    {
        // 1. Search by query (Ticker)
        var response = await _client.GetAsync("/api/instruments?query=AAPL");
        response.EnsureSuccessStatusCode();
        var instruments = await response.Content.ReadFromJsonAsync<List<InstrumentDto>>();
        
        instruments.Should().NotBeEmpty();
        instruments!.First().Ticker.Should().Be("AAPL");
        instruments.First().Name.Should().Be("APPLE");

        // 2. Filter by AssetType and Exchange
        response = await _client.GetAsync("/api/instruments?assetType=Stock&exchange=GPW");
        response.EnsureSuccessStatusCode();
        instruments = await response.Content.ReadFromJsonAsync<List<InstrumentDto>>();

        instruments.Should().NotBeEmpty();
        instruments!.All(i => i.AssetType == "Stock" && i.Exchange == "GPW").Should().BeTrue();
        instruments.Should().Contain(i => i.Ticker == "ALR"); // ALIOR

        // 3. Filter by AssetType only (Bond)
        response = await _client.GetAsync("/api/instruments?assetType=Bond");
        response.EnsureSuccessStatusCode();
        instruments = await response.Content.ReadFromJsonAsync<List<InstrumentDto>>();

        instruments.Should().NotBeEmpty();
        instruments!.All(i => i.AssetType == "Bond").Should().BeTrue();
    }
}
