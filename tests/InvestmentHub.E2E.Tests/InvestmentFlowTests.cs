using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace InvestmentHub.E2E.Tests;

public class InvestmentFlowTests : IAsyncLifetime
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
                    // Override configuration to use Testcontainers
                    // Note: In a real scenario, we might need a more robust way to override settings
                    // e.g. using environment variables or appsettings.Test.json
                    
                    // Workaround for .NET 9 TestServer PipeWriter issue:
                    // Use Newtonsoft.Json instead of System.Text.Json to avoid UnflushedBytes error
                    services.AddControllers()
                        .AddNewtonsoftJson();
                });

                builder.ConfigureLogging(logging => 
                {
                    logging.ClearProviders();
                    logging.AddConsole();
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
    public async Task FullFlow_Should_Create_Investment_And_Update_Portfolio()
    {
        // 0. Get a seeded user (database seeder creates test users)
        var usersResponse = await _client.GetAsync("/api/users");
        usersResponse.EnsureSuccessStatusCode();
        
        var users = await usersResponse.Content.ReadFromJsonAsync<List<UserDto>>();
        var testUser = users!.First(); // Use first seeded user
        var ownerId = testUser.Id;

        // 1. Create Portfolio
        var createPortfolioCommand = new { Name = "My Retirement Fund", OwnerId = ownerId };
        var portfolioResponse = await _client.PostAsJsonAsync("/api/portfolios", createPortfolioCommand);
        
        if (!portfolioResponse.IsSuccessStatusCode)
        {
            var content = await portfolioResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create portfolio. Status: {portfolioResponse.StatusCode}, Content: {content}");
        }
        
        var portfolio = await portfolioResponse.Content.ReadFromJsonAsync<PortfolioDto>();
        var portfolioId = portfolio!.Id;

        // 2. Add Investment
        var addInvestmentCommand = new AddInvestmentDto
        {
            PortfolioId = portfolioId,
            Symbol = "AAPL",
            PurchasePrice = 150.00m,
            Quantity = 10,
            Currency = "USD",
            PurchaseDate = DateTime.UtcNow
        };

        var investmentResponse = await _client.PostAsJsonAsync("/api/investments", addInvestmentCommand);
        
        if (!investmentResponse.IsSuccessStatusCode)
        {
            var errorContent = await investmentResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create investment. Status: {investmentResponse.StatusCode}, Content: {errorContent}");
        }
        
        investmentResponse.EnsureSuccessStatusCode();

        var investmentId = await investmentResponse.Content.ReadFromJsonAsync<Guid>();

        // 3. Verify Investment Created (Immediate Read Model update via API or eventual consistency?)
        // The API likely returns 202 Accepted or 201 Created.
        // We need to wait for the Worker to process the event and update the Read Model if it's async.
        // However, Marten inline projections might handle the initial creation.
        
        // Let's verify the portfolio total value is updated.
        // This happens in the Worker (InvestmentAddedConsumer) as per our previous implementation.
        
        // Poll for eventual consistency
        var expectedTotalValue = 1500.00m; // 150 * 10
        var maxRetries = 10;
        var delay = TimeSpan.FromSeconds(1);
        
        bool isUpdated = false;
        for (int i = 0; i < maxRetries; i++)
        {
            var getPortfolioResponse = await _client.GetAsync($"/api/portfolios/{portfolioId}");
            if (getPortfolioResponse.IsSuccessStatusCode)
            {
                var updatedPortfolio = await getPortfolioResponse.Content.ReadFromJsonAsync<PortfolioDto>();
                if (updatedPortfolio!.TotalValue == expectedTotalValue)
                {
                    isUpdated = true;
                    break;
                }
            }
            await Task.Delay(delay);
        }

        isUpdated.Should().BeTrue("Portfolio TotalValue should be updated by the Worker");
    }
}

// DTOs for testing
public class UserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

public class AddInvestmentDto
{
    public Guid PortfolioId { get; set; }
    public string Symbol { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal Quantity { get; set; }
    public string Currency { get; set; }
    public DateTime PurchaseDate { get; set; }
}

public class PortfolioDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public decimal TotalValue { get; set; }
}
