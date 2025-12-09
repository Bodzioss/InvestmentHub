using InvestmentHub.Domain.Services;

namespace InvestmentHub.Domain.Common;

/// <summary>
/// Extension methods for recording metrics from aggregate roots.
/// This simplifies metrics recording in command handlers.
/// </summary>
public static class AggregateRootMetricsExtensions
{
    /// <summary>
    /// Records business operation metrics for an aggregate.
    /// Records: business metric, events written, and projections updated.
    /// </summary>
    /// <typeparam name="T">The aggregate root type</typeparam>
    /// <param name="aggregate">The aggregate root instance</param>
    /// <param name="metricsRecorder">The metrics recorder service</param>
    /// <param name="recordBusinessMetric">Action to record the business metric (e.g., RecordPortfolioCreated)</param>
    /// <param name="projectionType">The projection type name (e.g., "PortfolioProjection", "InvestmentProjection")</param>
    public static void RecordMetrics<T>(
        this T aggregate,
        IMetricsRecorder metricsRecorder,
        Action<IMetricsRecorder> recordBusinessMetric,
        string projectionType) where T : AggregateRoot
    {
        var eventCount = aggregate.GetUncommittedEvents().Count;
        var aggregateType = typeof(T).Name;

        // Record business metric (e.g., portfolios.created, investments.added)
        recordBusinessMetric(metricsRecorder);

        // Record events written metric
        metricsRecorder.RecordEventsWritten(eventCount, aggregateType);

        // Record projections updated metric
        metricsRecorder.RecordProjectionUpdated(projectionType);
    }
}

