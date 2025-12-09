using InvestmentHub.Infrastructure.HealthChecks;
using InvestmentHub.Infrastructure.Resilience;
using Marten;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Polly;


namespace InvestmentHub.API.Extensions;

/// <summary>
/// Extension methods for configuring resilience and health checks
/// </summary>
public static class ResilienceExtensions
{
    /// <summary>
    /// Add resilience services to the service collection
    /// </summary>
    public static IServiceCollection AddResilienceServices(this IServiceCollection services)
    {
        // Register custom resilience service
        services.AddScoped<InvestmentHub.Domain.Services.IResilienceService, ResilienceService>();

        // Register standard Polly pipelines
        services.AddResiliencePipeline("default", builder =>
        {
            // Use a standard retry strategy for default
            builder.AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true
            });
        });

        return services;
    }

    /// <summary>
    /// Add health checks for all infrastructure dependencies
    /// </summary>
    public static IServiceCollection AddInfrastructureHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var healthChecksEnabled = configuration.GetValue<bool>("HealthChecks:Enabled", true);
        if (!healthChecksEnabled)
        {
            return services;
        }

        var healthChecksBuilder = services.AddHealthChecks();

        // PostgreSQL health check (custom implementation with Marten support)
        var postgresConnectionString = configuration.GetConnectionString("postgres");
        if (!string.IsNullOrEmpty(postgresConnectionString))
        {
            healthChecksBuilder.AddCheck(
                "postgresql",
                new DatabaseHealthCheck(
                    postgresConnectionString,
                    null), // IDocumentStore will be resolved at runtime via DI in CheckHealthAsync
                HealthStatus.Unhealthy,
                tags: new[] { "database", "postgresql" });
        }

        // RabbitMQ health check (custom implementation)
        var rabbitMqConnectionString = configuration["RabbitMQ:ConnectionString"];
        if (!string.IsNullOrEmpty(rabbitMqConnectionString))
        {
            // Ensure credentials are in the connection string
            if (!rabbitMqConnectionString.Contains("@") && rabbitMqConnectionString.StartsWith("amqp://"))
            {
                rabbitMqConnectionString = rabbitMqConnectionString.Replace("amqp://", "amqp://guest:guest@");
            }

            healthChecksBuilder.AddCheck(
                "rabbitmq",
                new RabbitMqHealthCheck(rabbitMqConnectionString),
                HealthStatus.Unhealthy,
                tags: new[] { "messaging", "rabbitmq" });
        }

        // Redis health check (custom implementation)
        var redisConnectionString = configuration.GetConnectionString("redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            healthChecksBuilder.AddCheck(
                "redis",
                new RedisHealthCheck(redisConnectionString),
                HealthStatus.Degraded,
                tags: new[] { "cache", "redis" });
        }

        // Yahoo Finance API health check
        healthChecksBuilder.AddCheck<YahooFinanceHealthCheck>(
            "yahoo_finance",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "external", "market-data" });

        // Add Health Checks UI
        services.AddHealthChecksUI(options =>
        {
            options.SetEvaluationTimeInSeconds(
                configuration.GetValue<int>("HealthChecks:EvaluationIntervalSeconds", 30));
            options.MaximumHistoryEntriesPerEndpoint(50);
            options.AddHealthCheckEndpoint("InvestmentHub API", "/health");
        })
        .AddInMemoryStorage();

        return services;
    }
}
