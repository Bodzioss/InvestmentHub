using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.Data;

public static class DemoDataSeeder
{
    private const string DemoEmail = "demo@investmenthub.com";
    private const string DemoPassword = "DemoUser123!";
    private const string DemoName = "Demo User";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DemoDataSeeder");

        try
        {
            var user = await userManager.FindByEmailAsync(DemoEmail);
            if (user != null)
            {
                logger.LogInformation("Demo user already exists. Skipping demo data seeding.");
                return;
            }

            logger.LogInformation("Creating demo user...");

            // 1. Create Identity User
            user = new ApplicationUser
            {
                UserName = DemoEmail,
                Email = DemoEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, DemoPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogError("Failed to create demo user: {Errors}", errors);
                return;
            }

            // 2. Create Domain User
            var userId = new UserId(user.Id);
            var domainUser = new User(userId, DemoName, DemoEmail, DateTime.UtcNow);
            context.DomainUsers.Add(domainUser);
            await context.SaveChangesAsync();

            logger.LogInformation("Demo user created successfully. Creating portfolios...");

            // 3. Create Portfolios
            var growthPortfolioId = PortfolioId.New();
            var retirementPortfolioId = PortfolioId.New();

            // Growth Portfolio
            await mediator.Send(new CreatePortfolioCommand(
                growthPortfolioId,
                userId,
                "Tech Growth",
                "High risk, high reward tech stocks",
                "USD"
            ));

            // Retirement Portfolio
            await mediator.Send(new CreatePortfolioCommand(
                retirementPortfolioId,
                userId,
                "Retirement Fund",
                "Safe dividend paying stocks and ETFs",
                "USD"
            ));

            logger.LogInformation("Portfolios created. Adding transactions...");

            // 4. Add Transactions to Growth Portfolio
            // Bought AAPL 1 year ago
            await mediator.Send(new RecordBuyTransactionCommand(
                growthPortfolioId,
                new Symbol("AAPL", "NASDAQ", AssetType.Stock),
                10,
                new Money(150.00m, Currency.USD), // Approx price
                DateTime.UtcNow.AddYears(-1),
                new Money(5.00m, Currency.USD), // Fee
                null,
                "Initial entry"
            ));

            // Bought NVDA 6 months ago
            await mediator.Send(new RecordBuyTransactionCommand(
                growthPortfolioId,
                new Symbol("NVDA", "NASDAQ", AssetType.Stock),
                5,
                new Money(400.00m, Currency.USD), // Pre-split massive gain simulation
                DateTime.UtcNow.AddMonths(-6),
                new Money(2.00m, Currency.USD),
                null,
                "AI Boom"
            ));

            // 5. Add Transactions to Retirement Portfolio
            // Bought VOO (S&P 500) 2 years ago
            await mediator.Send(new RecordBuyTransactionCommand(
                retirementPortfolioId,
                new Symbol("VOO", "NYSE", AssetType.ETF),
                20,
                new Money(350.00m, Currency.USD),
                DateTime.UtcNow.AddYears(-2),
                new Money(10.00m, Currency.USD),
                null,
                "Long term hold"
            ));

            // Bought KO (Coca Cola) 
            await mediator.Send(new RecordBuyTransactionCommand(
                retirementPortfolioId,
                new Symbol("KO", "NYSE", AssetType.Stock),
                50,
                new Money(55.00m, Currency.USD),
                DateTime.UtcNow.AddMonths(-18),
                new Money(5.00m, Currency.USD),
                null,
                "Dividend King"
            ));

             // Bought O (Realty Income)
            await mediator.Send(new RecordBuyTransactionCommand(
                retirementPortfolioId,
                new Symbol("O", "NYSE", AssetType.Stock),
                100,
                new Money(50.00m, Currency.USD),
                DateTime.UtcNow.AddMonths(-3),
                new Money(5.00m, Currency.USD),
                null,
                "Monthly Dividends"
            ));

            logger.LogInformation("Demo data seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding demo data.");
        }
    }
}
