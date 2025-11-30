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
            // Try to fetch a stable symbol like SPY (S&P 500 ETF) to verify API connectivity
            var security = await _yahooQuotes.GetSnapshotAsync("SPY", cancellationToken);

            if (security != null && security.RegularMarketPrice > 0)
            {
                return HealthCheckResult.Healthy("Yahoo Finance API is reachable and returning data.");
            }

            return HealthCheckResult.Degraded("Yahoo Finance API is reachable but returned no data for test symbol.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Yahoo Finance API is unreachable or throwing errors.", ex);
        }
    }
}
