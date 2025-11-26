namespace InvestmentHub.Domain.Services;

/// <summary>
/// Interface for recording business and technical metrics.
/// This allows Domain layer to record metrics without depending on Infrastructure.
/// </summary>
public interface IMetricsRecorder
{
    /// <summary>
    /// Records a portfolio created metric.
    /// </summary>
    void RecordPortfolioCreated();

    /// <summary>
    /// Records an investment added metric.
    /// </summary>
    void RecordInvestmentAdded();

    /// <summary>
    /// Records an investment updated metric.
    /// </summary>
    void RecordInvestmentUpdated();

    /// <summary>
    /// Records an investment sold metric.
    /// </summary>
    void RecordInvestmentSold();

    /// <summary>
    /// Records events written metric.
    /// </summary>
    /// <param name="eventCount">Number of events written</param>
    /// <param name="aggregateType">Type of aggregate (e.g., "PortfolioAggregate")</param>
    void RecordEventsWritten(int eventCount, string aggregateType);

    /// <summary>
    /// Records projections updated metric.
    /// </summary>
    /// <param name="projectionType">Type of projection (e.g., "PortfolioProjection")</param>
    void RecordProjectionUpdated(string projectionType);
}

