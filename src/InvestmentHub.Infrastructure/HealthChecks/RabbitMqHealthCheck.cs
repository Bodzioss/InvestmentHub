using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace InvestmentHub.Infrastructure.HealthChecks;

/// <summary>
/// Custom health check for RabbitMQ message broker
/// </summary>
public class RabbitMqHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    public RabbitMqHealthCheck(string connectionString)
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

            // Parse connection string (format: amqp://user:pass@host:port/)
            var uri = new Uri(_connectionString);
            
            var factory = new ConnectionFactory
            {
                Uri = uri,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
                SocketReadTimeout = TimeSpan.FromSeconds(5),
                SocketWriteTimeout = TimeSpan.FromSeconds(5)
            };

            // Use async API in RabbitMQ.Client v7.0.0
            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync();

            var responseTime = DateTime.UtcNow - startTime;

            // Verify connection is open
            if (!connection.IsOpen)
            {
                return HealthCheckResult.Unhealthy(
                    "RabbitMQ connection is not open",
                    null,
                    new Dictionary<string, object>
                    {
                        { "connectionString", MaskConnectionString(_connectionString) }
                    });
            }

            return HealthCheckResult.Healthy(
                "RabbitMQ is healthy",
                new Dictionary<string, object>
                {
                    { "responseTime", $"{responseTime.TotalMilliseconds}ms" },
                    { "connectionString", MaskConnectionString(_connectionString) },
                    { "isOpen", connection.IsOpen }
                });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "RabbitMQ is unhealthy",
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
            var uri = new Uri(connectionString);
            var userInfo = uri.UserInfo;
            if (!string.IsNullOrEmpty(userInfo))
            {
                var maskedUserInfo = userInfo.Contains(':') 
                    ? $"{userInfo.Split(':')[0]}:***" 
                    : userInfo;
                return connectionString.Replace(userInfo, maskedUserInfo);
            }
            return connectionString;
        }
        catch
        {
            return "***";
        }
    }
}
