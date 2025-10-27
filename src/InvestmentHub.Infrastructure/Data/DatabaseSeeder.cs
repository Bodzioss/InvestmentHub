using Microsoft.EntityFrameworkCore;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Enums;

namespace InvestmentHub.Infrastructure.Data;

/// <summary>
/// Database seeder for initial data.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Seeds the database with initial data.
    /// </summary>
    /// <param name="context">The database context</param>
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Check if data already exists
        if (await context.Users.AnyAsync())
        {
            return; // Data already seeded
        }

        // Create sample users
        var users = new List<User>
        {
            new User(
                UserId.New(),
                "Jan Kowalski",
                "jan.kowalski@example.com",
                DateTime.UtcNow.AddDays(-30)
            ),
            new User(
                UserId.New(),
                "Anna Nowak",
                "anna.nowak@example.com",
                DateTime.UtcNow.AddDays(-25)
            ),
            new User(
                UserId.New(),
                "Piotr Wiśniewski",
                "piotr.wisniewski@example.com",
                DateTime.UtcNow.AddDays(-20)
            )
        };

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();

        // Create sample portfolios
        var portfolios = new List<Portfolio>
        {
            new Portfolio(
                PortfolioId.New(),
                "Główny Portfel",
                "Mój główny portfel inwestycyjny z długoterminowymi inwestycjami",
                users[0].Id
            ),
            new Portfolio(
                PortfolioId.New(),
                "Portfel Agresywny",
                "Portfel z wysokim ryzykiem i wysokim potencjałem zysku",
                users[0].Id
            ),
            new Portfolio(
                PortfolioId.New(),
                "Portfel Konserwatywny",
                "Bezpieczny portfel z niskim ryzykiem",
                users[1].Id
            ),
            new Portfolio(
                PortfolioId.New(),
                "Portfel ETF",
                "Portfel skupiony na funduszach ETF",
                users[2].Id
            )
        };

        await context.Portfolios.AddRangeAsync(portfolios);
        await context.SaveChangesAsync();

        // Create sample investments
        var investments = new List<Investment>
        {
            // Investments for Jan Kowalski - Główny Portfel
            new Investment(
                InvestmentId.New(),
                portfolios[0].Id,
                new Symbol("AAPL", "NASDAQ", AssetType.Stock),
                new Money(150.00m, Currency.USD),
                10m,
                DateTime.UtcNow.AddDays(-60)
            ),
            new Investment(
                InvestmentId.New(),
                portfolios[0].Id,
                new Symbol("MSFT", "NASDAQ", AssetType.Stock),
                new Money(300.00m, Currency.USD),
                5m,
                DateTime.UtcNow.AddDays(-45)
            ),
            new Investment(
                InvestmentId.New(),
                portfolios[0].Id,
                new Symbol("GOOGL", "NASDAQ", AssetType.Stock),
                new Money(2500.00m, Currency.USD),
                2m,
                DateTime.UtcNow.AddDays(-30)
            ),

            // Investments for Jan Kowalski - Portfel Agresywny
            new Investment(
                InvestmentId.New(),
                portfolios[1].Id,
                new Symbol("TSLA", "NASDAQ", AssetType.Stock),
                new Money(200.00m, Currency.USD),
                25m,
                DateTime.UtcNow.AddDays(-20)
            ),
            new Investment(
                InvestmentId.New(),
                portfolios[1].Id,
                new Symbol("NVDA", "NASDAQ", AssetType.Stock),
                new Money(400.00m, Currency.USD),
                10m,
                DateTime.UtcNow.AddDays(-15)
            ),

            // Investments for Anna Nowak - Portfel Konserwatywny
            new Investment(
                InvestmentId.New(),
                portfolios[2].Id,
                new Symbol("JNJ", "NYSE", AssetType.Stock),
                new Money(160.00m, Currency.USD),
                20m,
                DateTime.UtcNow.AddDays(-40)
            ),
            new Investment(
                InvestmentId.New(),
                portfolios[2].Id,
                new Symbol("PG", "NYSE", AssetType.Stock),
                new Money(140.00m, Currency.USD),
                15m,
                DateTime.UtcNow.AddDays(-35)
            ),

            // Investments for Piotr Wiśniewski - Portfel ETF
            new Investment(
                InvestmentId.New(),
                portfolios[3].Id,
                new Symbol("SPY", "NYSE", AssetType.ETF),
                new Money(400.00m, Currency.USD),
                5m,
                DateTime.UtcNow.AddDays(-25)
            ),
            new Investment(
                InvestmentId.New(),
                portfolios[3].Id,
                new Symbol("QQQ", "NASDAQ", AssetType.ETF),
                new Money(350.00m, Currency.USD),
                8m,
                DateTime.UtcNow.AddDays(-20)
            )
        };

        // Update current values to simulate market changes
        foreach (var investment in investments)
        {
            // Simulate some price changes
            var priceChange = Random.Shared.Next(-20, 30) / 100m; // -20% to +30%
            var currentPricePerUnit = investment.PurchasePrice.Amount * (1 + priceChange);
            investment.UpdateCurrentValue(new Money(currentPricePerUnit, investment.PurchasePrice.Currency));
        }

        await context.Investments.AddRangeAsync(investments);
        await context.SaveChangesAsync();

        Console.WriteLine("Database seeded successfully with sample data!");
    }
}
