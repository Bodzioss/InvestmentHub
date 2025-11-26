using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Custom Serilog enricher that adds OpenTelemetry TraceId and SpanId to log entries.
/// This enables correlation between logs (Seq) and traces (Jaeger) using TraceId.
/// </summary>
public class ActivityTraceEnricher : ILogEventEnricher
{
    /// <summary>
    /// Enriches the log event with TraceId and SpanId from the current Activity.
    /// </summary>
    /// <param name="logEvent">The log event to enrich</param>
    /// <param name="propertyFactory">Factory for creating log properties</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        
        if (activity != null)
        {
            // Add TraceId (full W3C trace context format: 00-{trace-id}-{span-id}-{flags})
            // This is the complete trace identifier that can be used to find the trace in Jaeger
            var traceId = activity.Id;
            if (!string.IsNullOrWhiteSpace(traceId))
            {
                logEvent.AddPropertyIfAbsent(
                    propertyFactory.CreateProperty("TraceId", traceId));
            }

            // Add SpanId (the current span identifier)
            // This identifies the specific operation/span within the trace
            var spanId = activity.SpanId.ToString();
            if (!string.IsNullOrWhiteSpace(spanId) && spanId != "0000000000000000")
            {
                logEvent.AddPropertyIfAbsent(
                    propertyFactory.CreateProperty("SpanId", spanId));
            }

            // Also add the trace ID in W3C format (just the trace-id part, without span-id and flags)
            // This is useful for filtering in Seq/Jaeger
            var traceIdHex = activity.TraceId.ToString();
            if (!string.IsNullOrWhiteSpace(traceIdHex) && traceIdHex != "00000000000000000000000000000000")
            {
                logEvent.AddPropertyIfAbsent(
                    propertyFactory.CreateProperty("TraceIdHex", traceIdHex));
            }
        }
    }
}

