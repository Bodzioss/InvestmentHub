using InvestmentHub.Domain.Services;
using Microsoft.Extensions.Hosting;

namespace InvestmentHub.Infrastructure.Metrics;

/// <summary>
/// Implementation of IMetricsRecorder that records metrics using InvestmentHubMetrics.
/// </summary>
public class MetricsRecorder : IMetricsRecorder
{
    /// <summary>
    /// Records a portfolio created metric.
    /// </summary>
    public void RecordPortfolioCreated()
    {
        InvestmentHubMetrics.PortfoliosCreated.Add(1, new KeyValuePair<string, object?>("operation", "CreatePortfolio"));
    }

    /// <summary>
    /// Records an investment added metric.
    /// </summary>
    public void RecordInvestmentAdded()
    {
        InvestmentHubMetrics.InvestmentsAdded.Add(1, new KeyValuePair<string, object?>("operation", "AddInvestment"));
    }

    /// <summary>
    /// Records an investment updated metric.
    /// </summary>
    public void RecordInvestmentUpdated()
    {
        InvestmentHubMetrics.InvestmentsUpdated.Add(1, new KeyValuePair<string, object?>("operation", "UpdateInvestmentValue"));
    }

    /// <summary>
    /// Records an investment sold metric.
    /// </summary>
    public void RecordInvestmentSold()
    {
        InvestmentHubMetrics.InvestmentsSold.Add(1, new KeyValuePair<string, object?>("operation", "SellInvestment"));
    }

    /// <summary>
    /// Records events written metric.
    /// </summary>
    /// <param name="eventCount">Number of events written</param>
    /// <param name="aggregateType">Type of aggregate (e.g., "PortfolioAggregate")</param>
    public void RecordEventsWritten(int eventCount, string aggregateType)
    {
        if (eventCount > 0)
        {
            InvestmentHubMetrics.EventsWritten.Add(
                eventCount,
                new KeyValuePair<string, object?>("aggregate", aggregateType));
        }
    }

    /// <summary>
    /// Records projections updated metric.
    /// </summary>
    /// <param name="projectionType">Type of projection (e.g., "PortfolioProjection")</param>
    public void RecordProjectionUpdated(string projectionType)
    {
        InvestmentHubMetrics.ProjectionsUpdated.Add(
            1,
            new KeyValuePair<string, object?>("projection", projectionType));
    }
}

