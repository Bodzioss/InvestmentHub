using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace InvestmentHub.Infrastructure.HealthChecks;

/// <summary>
/// Custom health check for Redis cache
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    public RedisHealthCheck(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var startTime = DateTime.UtcNow;

            var options = ConfigurationOptions.Parse(_connectionString);
            options.ConnectTimeout = 5000; // 5 seconds
            options.SyncTimeout = 5000;

            await using var connection = await ConnectionMultiplexer.ConnectAsync(options);
            var database = connection.GetDatabase();

            // Test read/write operations
            var testKey = "__health_check__";
            var testValue = DateTime.UtcNow.Ticks.ToString();
            
            await database.StringSetAsync(testKey, testValue, TimeSpan.FromSeconds(10));
            var retrievedValue = await database.StringGetAsync(testKey);
            await database.KeyDeleteAsync(testKey);

            var responseTime = DateTime.UtcNow - startTime;

            if (retrievedValue != testValue)
            {
                return HealthCheckResult.Degraded(
                    "Redis cache read/write verification failed",
                    null,
                    new Dictionary<string, object>
                    {
                        { "connectionString", MaskConnectionString(_connectionString) },
                        { "responseTime", $"{responseTime.TotalMilliseconds}ms" }
                    });
            }

            return HealthCheckResult.Healthy(
                "Redis cache is healthy",
                new Dictionary<string, object>
                {
                    { "responseTime", $"{responseTime.TotalMilliseconds}ms" },
                    { "connectionString", MaskConnectionString(_connectionString) },
                    { "isConnected", connection.IsConnected }
                });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Redis cache is unhealthy",
                ex,
                new Dictionary<string, object>
                {
                    { "connectionString", MaskConnectionString(_connectionString) },
                    { "error", ex.Message }
                });
        }
    }

    private static string MaskConnectionString(string connectionString)
    {
        try
        {
            var options = ConfigurationOptions.Parse(connectionString);
            if (!string.IsNullOrEmpty(options.Password))
            {
                options.Password = "***";
            }
            return options.ToString();
        }
        catch
        {
            return "***";
        }
    }
}
