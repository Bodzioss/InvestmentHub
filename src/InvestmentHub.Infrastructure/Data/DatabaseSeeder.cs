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
    public static async Task SeedAsync(IServiceProvider serviceProvider, YahooQuotesApi.YahooQuotes yahooQuotes, InvestmentHub.Infrastructure.Jobs.HistoricalImportJob historicalImportJob)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // 1. Apply Migrations - REMOVED
        // Migrations are now handled and protected in Program.cs. 
        // We do strictly seeding here.

        // 2. Seed Core Data (Users/Portfolios) (Synchronous - Fast & Required)
        // Seed Demo User and Data
        await DemoDataSeeder.SeedAsync(serviceProvider);

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

        // 4. Trigger Historical Import for Seeded Portfolios (Background)
        // We do this after instruments start importing so that context is available, 
        // though imports are independent.
        _ = Task.Run(async () =>
        {
            try
            {
                // Create scope for history job execution if needed, but here we reuse the job singleton/scoped service from main scope
                // or create new scope if it depends on DbContext.
                // HistoricalImportJob takes DbContext, so we need a scope.
                using var historyScope = serviceProvider.CreateScope();
                var job = historyScope.ServiceProvider.GetRequiredService<Jobs.HistoricalImportJob>();

                // Get portfolio IDs from DB using the scope
                // Get portfolio IDs from Marten Read Side (since CQRS writes there)
                // var context = historyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); 
                // var portfolios = await context.Portfolios.ToListAsync(); // This reads from EF Core table which might be empty/stale

                // Use Marten QuerySession via IDocumentStore to be safe
                var store = historyScope.ServiceProvider.GetRequiredService<global::Marten.IDocumentStore>();
                using var querySession = store.QuerySession();
                var portfolios = querySession.Query<Domain.ReadModels.PortfolioReadModel>().ToList();

                foreach (var portfolio in portfolios)
                {
                    Console.WriteLine($"Starting background history import for portfolio: {portfolio.Name}...");
                    await job.ImportPortfolioHistoryAsync(portfolio.Id);
                }
                Console.WriteLine("Background history import completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Background history import failed: {ex.Message}");
            }
        });
    }
}
