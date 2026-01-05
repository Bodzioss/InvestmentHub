using Microsoft.EntityFrameworkCore;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentHub.Infrastructure.Data;

/// <summary>
/// Database seeder for initial data.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Seeds the database with initial data.
    /// </summary>
    /// <param name="serviceProvider">The service provider to create scopes for background tasks</param>
    /// <param name="yahooQuotes">The YahooQuotes service</param>
    public static async Task SeedAsync(IServiceProvider serviceProvider, YahooQuotesApi.YahooQuotes yahooQuotes)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // 1. Apply Migrations (Synchronous - Required)
        // NOTE: If this fails with "relation already exists", it means the database was created with EnsureCreatedAsync previously.
        await context.Database.MigrateAsync();

        // 2. Seed Core Data (Users/Portfolios) (Synchronous - Fast & Required)
        if (!await context.DomainUsers.AnyAsync())
        {
            var users = new List<User>
            {
                new User(UserId.New(), "Jan Kowalski", "jan.kowalski@example.com", DateTime.UtcNow.AddDays(-30)),
                new User(UserId.New(), "Anna Nowak", "anna.nowak@example.com", DateTime.UtcNow.AddDays(-25)),
                new User(UserId.New(), "Piotr Wiśniewski", "piotr.wisniewski@example.com", DateTime.UtcNow.AddDays(-20))
            };
            await context.DomainUsers.AddRangeAsync(users);
            await context.SaveChangesAsync();

            var portfolios = new List<Portfolio>
            {
                new Portfolio(PortfolioId.New(), "Główny Portfel", "Mój główny portfel inwestycyjny z długoterminowymi inwestycjami", users[0].Id),
                new Portfolio(PortfolioId.New(), "Portfel Agresywny", "Portfel z wysokim ryzykiem i wysokim potencjałem zysku", users[0].Id),
                new Portfolio(PortfolioId.New(), "Portfel Konserwatywny", "Bezpieczny portfel z niskim ryzykiem", users[1].Id),
                new Portfolio(PortfolioId.New(), "Portfel ETF", "Portfel skupiony na funduszach ETF", users[2].Id)
            };
            await context.Portfolios.AddRangeAsync(portfolios);
            await context.SaveChangesAsync();

            var investments = new List<Investment>
            {
                // Jan Kowalski - Główny Portfel
                new Investment(InvestmentId.New(), portfolios[0].Id, new Symbol("AAPL", "NASDAQ", AssetType.Stock), new Money(150.00m, Currency.USD), 10m, DateTime.UtcNow.AddDays(-60)),
                new Investment(InvestmentId.New(), portfolios[0].Id, new Symbol("MSFT", "NASDAQ", AssetType.Stock), new Money(300.00m, Currency.USD), 5m, DateTime.UtcNow.AddDays(-45)),
                new Investment(InvestmentId.New(), portfolios[0].Id, new Symbol("GOOGL", "NASDAQ", AssetType.Stock), new Money(2500.00m, Currency.USD), 2m, DateTime.UtcNow.AddDays(-30)),

                // Jan Kowalski - Portfel Agresywny
                new Investment(InvestmentId.New(), portfolios[1].Id, new Symbol("TSLA", "NASDAQ", AssetType.Stock), new Money(200.00m, Currency.USD), 25m, DateTime.UtcNow.AddDays(-20)),
                new Investment(InvestmentId.New(), portfolios[1].Id, new Symbol("NVDA", "NASDAQ", AssetType.Stock), new Money(400.00m, Currency.USD), 10m, DateTime.UtcNow.AddDays(-15)),

                // Anna Nowak - Portfel Konserwatywny
                new Investment(InvestmentId.New(), portfolios[2].Id, new Symbol("JNJ", "NYSE", AssetType.Stock), new Money(160.00m, Currency.USD), 20m, DateTime.UtcNow.AddDays(-40)),
                new Investment(InvestmentId.New(), portfolios[2].Id, new Symbol("PG", "NYSE", AssetType.Stock), new Money(140.00m, Currency.USD), 15m, DateTime.UtcNow.AddDays(-35)),

                // Piotr Wiśniewski - Portfel ETF
                new Investment(InvestmentId.New(), portfolios[3].Id, new Symbol("SPY", "NYSE", AssetType.ETF), new Money(400.00m, Currency.USD), 5m, DateTime.UtcNow.AddDays(-25)),
                new Investment(InvestmentId.New(), portfolios[3].Id, new Symbol("QQQ", "NASDAQ", AssetType.ETF), new Money(350.00m, Currency.USD), 8m, DateTime.UtcNow.AddDays(-20))
            };

            // Update current values
            foreach (var investment in investments)
            {
                var priceChange = Random.Shared.Next(-20, 30) / 100m;
                var currentPricePerUnit = investment.PurchasePrice.Amount * (1 + priceChange);
                investment.UpdateCurrentValue(new Money(currentPricePerUnit, investment.PurchasePrice.Currency));
            }

            await context.Investments.AddRangeAsync(investments);
            await context.SaveChangesAsync();

            Console.WriteLine("Core database seeded successfully with sample data!");
        }

        // 3. Import Instruments (Background - runs after app starts)
        _ = Task.Run(async () =>
        {
            try
            {
                // Create a NEW scope for the background thread
                using var bgScope = serviceProvider.CreateScope();
                var bgContext = bgScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var importer = new InstrumentImporter(bgContext, yahooQuotes);
                var instrumentFilePath = Path.Combine(AppContext.BaseDirectory, "all_instruments_list.json");

                if (File.Exists(instrumentFilePath))
                {
                    Console.WriteLine("Starting background import of GPW instruments...");
                    await importer.ImportAsync(instrumentFilePath);
                }
                else
                {
                    Console.WriteLine($"Warning: GPW Instrument file not found at {instrumentFilePath}. Skipping import.");
                }

                var globalInstrumentFilePath = Path.Combine(AppContext.BaseDirectory, "valid_global_instruments.json");
                if (File.Exists(globalInstrumentFilePath))
                {
                    Console.WriteLine("Starting background import of Global instruments...");
                    await importer.ImportGlobalAsync(globalInstrumentFilePath);
                }

                var etfsFilePath = Path.Combine(AppContext.BaseDirectory, "ETFsList.csv");
                if (File.Exists(etfsFilePath))
                {
                    Console.WriteLine("Starting background import of ETFs from CSV...");
                    await importer.ImportEtfsCsvAsync(etfsFilePath);
                }

                Console.WriteLine("Background instrument import completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Background import failed: {ex.Message}");
            }
        });
    }
}
