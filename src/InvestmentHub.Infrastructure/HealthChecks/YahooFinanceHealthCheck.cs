using Microsoft.Extensions.Diagnostics.HealthChecks;
using YahooQuotesApi;

namespace InvestmentHub.Infrastructure.HealthChecks;

public class YahooFinanceHealthCheck : IHealthCheck
{
    private readonly YahooQuotes _yahooQuotes;

    public YahooFinanceHealthCheck(YahooQuotes yahooQuotes)
    {
        _yahooQuotes = yahooQuotes;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use a short timeout (3s) to prevent blocking the health check thread for too long
            // This prevents Azure from marking the app as Unhealthy due to slow third-party API
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));

            // Try to fetch a stable symbol like SPY (S&P 500 ETF) to verify API connectivity
            var security = await _yahooQuotes.GetSnapshotAsync("SPY", cts.Token);

            if (security != null && security.RegularMarketPrice > 0)
            {
                return HealthCheckResult.Healthy("Yahoo Finance API is reachable and returning data.");
            }

            return HealthCheckResult.Degraded("Yahoo Finance API is reachable but returned no data for test symbol.");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Degraded("Yahoo Finance API check timed out (slow response).");
        }
        catch (Exception ex)
        {
            // Return Degraded (Warning) instead of Unhealthy (Error)
            // Failing to get market data shouldn't kill the entire application
            return HealthCheckResult.Degraded($"Yahoo Finance API is unreachable: {ex.Message}", ex);
        }
    }
}
