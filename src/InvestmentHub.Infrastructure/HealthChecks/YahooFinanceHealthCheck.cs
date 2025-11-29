using Microsoft.Extensions.Diagnostics.HealthChecks;
using YahooFinanceApi;

namespace InvestmentHub.Infrastructure.HealthChecks;

public class YahooFinanceHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to fetch a stable symbol like SPY (S&P 500 ETF) to verify API connectivity
            var securities = await Yahoo.Symbols("SPY")
                .Fields(Field.Symbol, Field.RegularMarketPrice)
                .QueryAsync(cancellationToken);

            var security = securities.Values.FirstOrDefault();

            if (security != null)
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
