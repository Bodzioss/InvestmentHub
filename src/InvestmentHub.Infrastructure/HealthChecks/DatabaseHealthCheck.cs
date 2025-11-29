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
            var startTime = DateTime.UtcNow;

            // Test basic PostgreSQL connection
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            
            // Execute a simple query to verify database is responsive
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);

            var responseTime = DateTime.UtcNow - startTime;

            // Test Marten event store if available
            if (_documentStore != null)
            {
                await using var session = _documentStore.LightweightSession();
                // Simple query to verify Marten is working
                await session.Query<object>().Take(1).ToListAsync(cancellationToken);
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
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "PostgreSQL database is unhealthy",
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
