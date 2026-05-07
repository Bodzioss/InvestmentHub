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
        .WithImage("pgvector/pgvector:pg16")
        .Build();

    private readonly RabbitMqContainer _rabbitmq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management")
        .Build();

    private WebApplicationFactory<Program> _apiFactory = null!;
    private HttpClient _client = null!;

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
        // Poll for availability (background seeding)
        List<InstrumentDto>? instruments = null;
        for (int i = 0; i < 60; i++)
        {
            var searchResponse = await _client.GetAsync("/api/instruments?query=AAPL");
            if (searchResponse.IsSuccessStatusCode)
            {
                var pollResult = await searchResponse.Content.ReadFromJsonAsync<InstrumentResponseDto>();
                instruments = pollResult?.Instruments;
                if (instruments != null && instruments.Count > 0)
                    break;
            }
            await Task.Delay(500);
        }

        var response = await _client.GetAsync("/api/instruments?query=AAPL");
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to search instruments. Status: {response.StatusCode}. Content: {content}");
        }
        var result = await response.Content.ReadFromJsonAsync<InstrumentResponseDto>();
        instruments = result?.Instruments;
        
        instruments!.Should().NotBeEmpty();
        instruments![0].Ticker.Should().Be("AAPL");
        instruments[0].Name.Should().Be("APPLE");

        // 2. Filter by AssetType and Exchange
        response = await _client.GetAsync("/api/instruments?assetType=Stock&exchange=GPW");
        response.EnsureSuccessStatusCode();
        var result2 = await response.Content.ReadFromJsonAsync<InstrumentResponseDto>();
        instruments = result2?.Instruments;

        instruments.Should().NotBeEmpty();
        instruments!.All(i => i.AssetType == "Stock" && i.Exchange == "GPW").Should().BeTrue();
        instruments.Should().Contain(i => i.Ticker == "ALR"); // ALIOR

        // 3. Filter by AssetType only (Bond)
        response = await _client.GetAsync("/api/instruments?assetType=Bond");
        response.EnsureSuccessStatusCode();
        var result3 = await response.Content.ReadFromJsonAsync<InstrumentResponseDto>();
        instruments = result3?.Instruments;

        instruments.Should().NotBeEmpty();
        instruments!.All(i => i.AssetType == "Bond").Should().BeTrue();
    }
}

public class InstrumentResponseDto
{
    public List<InstrumentDto> Instruments { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
