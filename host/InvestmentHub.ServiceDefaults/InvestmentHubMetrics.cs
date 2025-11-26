using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Custom metrics for InvestmentHub application.
/// These metrics are automatically exported to Aspire Dashboard via OpenTelemetry.
/// </summary>
public static class InvestmentHubMetrics
{
    /// <summary>
    /// Meter name for InvestmentHub metrics.
    /// This must be registered in OpenTelemetry configuration.
    /// </summary>
    public const string MeterName = "InvestmentHub.Metrics";

    /// <summary>
    /// Meter instance for creating custom metrics.
    /// </summary>
    public static readonly Meter Meter = new(MeterName, "1.0.0");

    // Business Metrics - Counters
    /// <summary>
    /// Counter for portfolios created.
    /// </summary>
    public static readonly Counter<long> PortfoliosCreated = Meter.CreateCounter<long>(
        name: "investmenthub.portfolios.created",
        unit: "portfolios",
        description: "Total number of portfolios created");

    /// <summary>
    /// Counter for investments added.
    /// </summary>
    public static readonly Counter<long> InvestmentsAdded = Meter.CreateCounter<long>(
        name: "investmenthub.investments.added",
        unit: "investments",
        description: "Total number of investments added");

    /// <summary>
    /// Counter for investments updated.
    /// </summary>
    public static readonly Counter<long> InvestmentsUpdated = Meter.CreateCounter<long>(
        name: "investmenthub.investments.updated",
        unit: "investments",
        description: "Total number of investments updated");

    /// <summary>
    /// Counter for investments sold.
    /// </summary>
    public static readonly Counter<long> InvestmentsSold = Meter.CreateCounter<long>(
        name: "investmenthub.investments.sold",
        unit: "investments",
        description: "Total number of investments sold");

    // Technical Metrics - Counters
    /// <summary>
    /// Counter for events written to Marten.
    /// </summary>
    public static readonly Counter<long> EventsWritten = Meter.CreateCounter<long>(
        name: "investmenthub.events.written",
        unit: "events",
        description: "Total number of events written to Marten event store");

    /// <summary>
    /// Counter for projections updated.
    /// </summary>
    public static readonly Counter<long> ProjectionsUpdated = Meter.CreateCounter<long>(
        name: "investmenthub.projections.updated",
        unit: "projections",
        description: "Total number of projections updated");

    // Technical Metrics - Histograms (for duration measurements)
    /// <summary>
    /// Histogram for command execution duration.
    /// </summary>
    public static readonly Histogram<double> CommandDuration = Meter.CreateHistogram<double>(
        name: "investmenthub.command.duration",
        unit: "ms",
        description: "Duration of command execution in milliseconds");

    /// <summary>
    /// Histogram for Marten operation duration.
    /// </summary>
    public static readonly Histogram<double> MartenOperationDuration = Meter.CreateHistogram<double>(
        name: "investmenthub.marten.operation.duration",
        unit: "ms",
        description: "Duration of Marten operations in milliseconds");
}

