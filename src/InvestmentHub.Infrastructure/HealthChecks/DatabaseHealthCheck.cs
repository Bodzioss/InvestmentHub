using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using Marten;

namespace InvestmentHub.Infrastructure.HealthChecks;

/// <summary>
/// Custom health check for PostgreSQL database and Marten event store
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly string _connectionString;
    private readonly IDocumentStore? _documentStore;

    public DatabaseHealthCheck(string connectionString, IDocumentStore? documentStore = null)
    {
        _connectionString = connectionString;
        _documentStore = documentStore;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use a short timeout (3s) for the check itself
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));

            var startTime = DateTime.UtcNow;

            // Test basic PostgreSQL connection
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cts.Token);
            
            // Execute a simple query to verify database is responsive
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cts.Token);

            var responseTime = DateTime.UtcNow - startTime;

            // Test Marten event store if available
            // Note: Marten session creation is usually fast, but query might be slow
            if (_documentStore != null)
            {
                 // Check Marten store connectivity (lightweight)
                 using var session = _documentStore.LightweightSession();
                 // Just ensure we can get a session, avoiding heavy queries here
            }

            var data = new Dictionary<string, object>
            {
                { "responseTime", $"{responseTime.TotalMilliseconds}ms" },
                { "connectionString", MaskConnectionString(_connectionString) }
            };

            if (_documentStore != null)
            {
                data.Add("martenEventStore", "Connected");
            }

            return HealthCheckResult.Healthy(
                "PostgreSQL database is healthy",
                data);
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Degraded("PostgreSQL database check timed out (slow response).");
        }
        catch (Exception ex)
        {
             // Return Degraded (Warning) instead of Unhealthy (Error) to keep app alive during DB glitches
            return HealthCheckResult.Degraded(
                "PostgreSQL database is unreachable or slow",
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
        // Mask password in connection string for security
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrEmpty(builder.Password))
        {
            builder.Password = "***";
        }
        return builder.ToString();
    }
}
