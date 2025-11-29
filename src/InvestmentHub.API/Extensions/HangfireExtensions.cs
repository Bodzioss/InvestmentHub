using Hangfire;
using Hangfire.PostgreSql;

namespace InvestmentHub.API.Extensions;

public static class HangfireExtensions
{
    public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("postgres");

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));

        // Add the processing server as IHostedService
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 1; // Limit to 1 worker for free tier/dev
        });

        return services;
    }

    public static IApplicationBuilder UseHangfireDashboard(this IApplicationBuilder app)
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            DashboardTitle = "InvestmentHub Jobs",
            Authorization = new[] { new HangfireAuthorizationFilter() } // We'll implement a simple filter
        });

        // Schedule recurring jobs
        RecurringJob.AddOrUpdate<InvestmentHub.Infrastructure.Jobs.PriceUpdateJob>(
            "update-active-prices",
            job => job.UpdateActivePricesAsync(),
            "*/15 * * * *"); // Every 15 minutes

        return app;
    }
}

// Simple authorization filter for dev (allow local requests)
public class HangfireAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        // In production, implement proper auth. For now, allow all or local.
        return true; 
    }
}
