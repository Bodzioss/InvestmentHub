using System.Diagnostics;
using Marten;
using Marten.Events;

namespace InvestmentHub.Domain.Common;

/// <summary>
/// Static ActivitySource for Marten operations tracing.
/// This ensures all Marten operations are properly traced in Jaeger.
/// </summary>
public static class MartenTracing
{
    /// <summary>
    /// ActivitySource for Marten operations.
    /// This must be registered in OpenTelemetry configuration.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new("InvestmentHub.Marten");

    /// <summary>
    /// Gets Correlation ID from the current Activity or parent Activity.
    /// Correlation ID is set in CorrelationIdMiddleware and propagated through Activity.Current.
    /// </summary>
    /// <returns>Correlation ID string or null if not found</returns>
    public static string? GetCorrelationIdFromActivity()
    {
        var activity = Activity.Current;
        while (activity != null)
        {
            // Check if Correlation ID is set as a tag in this activity
            var correlationId = activity.GetTagItem("correlation.id")?.ToString();
            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                return correlationId;
            }
            
            // Move to parent activity
            activity = activity.Parent;
        }
        
        return null;
    }
}

/// <summary>
/// Extension methods for Marten IEventStore with automatic OpenTelemetry tracing.
/// These methods wrap Marten operations with tracing to eliminate "UNKNOWN" spans in Jaeger.
/// </summary>
public static class MartenEventStoreExtensions
{
    /// <summary>
    /// Starts an event stream with automatic OpenTelemetry tracing.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate type</typeparam>
    /// <param name="eventStore">The event store</param>
    /// <param name="streamId">The stream ID</param>
    /// <param name="events">The events to append</param>
    public static void StartStreamWithTracing<TAggregate>(
        this IEventStore eventStore,
        Guid streamId,
        object[] events)
        where TAggregate : class
    {
        var aggregateTypeName = typeof(TAggregate).Name;
        using var activity = MartenTracing.ActivitySource.StartActivity($"Marten.StartStream.{aggregateTypeName}");
        
        activity?.SetTag("marten.stream.id", streamId.ToString());
        activity?.SetTag("marten.stream.type", aggregateTypeName);
        activity?.SetTag("marten.events.count", events.Length);
        
        // Add Correlation ID to activity if available from parent Activity
        var correlationId = MartenTracing.GetCorrelationIdFromActivity();
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            activity?.SetTag("correlation.id", correlationId);
        }
        
        eventStore.StartStream<TAggregate>(streamId, events);
    }

    /// <summary>
    /// Appends events to a stream with automatic OpenTelemetry tracing.
    /// </summary>
    /// <param name="eventStore">The event store</param>
    /// <param name="streamId">The stream ID</param>
    /// <param name="events">The events to append</param>
    public static void AppendWithTracing(
        this IEventStore eventStore,
        Guid streamId,
        object[] events)
    {
        using var activity = MartenTracing.ActivitySource.StartActivity("Marten.Append");
        
        activity?.SetTag("marten.stream.id", streamId.ToString());
        activity?.SetTag("marten.events.count", events.Length);
        
        // Add Correlation ID to activity if available from parent Activity
        var correlationId = MartenTracing.GetCorrelationIdFromActivity();
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            activity?.SetTag("correlation.id", correlationId);
        }
        
        eventStore.Append(streamId, events);
    }

    /// <summary>
    /// Fetches a stream with automatic OpenTelemetry tracing.
    /// </summary>
    /// <param name="eventStore">The event store</param>
    /// <param name="streamId">The stream ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The events in the stream</returns>
    public static async Task<IReadOnlyList<IEvent>> FetchStreamWithTracingAsync(
        this IEventStore eventStore,
        Guid streamId,
        CancellationToken cancellationToken = default)
    {
        using var activity = MartenTracing.ActivitySource.StartActivity("Marten.FetchStream");
        
        activity?.SetTag("marten.stream.id", streamId.ToString());
        
        // Add Correlation ID to activity if available from parent Activity
        var correlationId = MartenTracing.GetCorrelationIdFromActivity();
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            activity?.SetTag("correlation.id", correlationId);
        }
        
        try
        {
            var events = await eventStore.FetchStreamAsync(streamId, token: cancellationToken);
            activity?.SetTag("marten.events.count", events?.Count ?? 0);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return events ?? Array.Empty<IEvent>();
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error", true);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            throw;
        }
    }
}

/// <summary>
/// Extension methods for Marten IDocumentSession with automatic OpenTelemetry tracing.
/// </summary>
public static class MartenDocumentSessionExtensions
{
    /// <summary>
    /// Saves changes with automatic OpenTelemetry tracing.
    /// </summary>
    /// <param name="session">The document session</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public static async Task SaveChangesWithTracingAsync(
        this IDocumentSession session,
        CancellationToken cancellationToken = default)
    {
        using var activity = MartenTracing.ActivitySource.StartActivity("Marten.SaveChanges");
        
        // Add Correlation ID to activity if available from parent Activity
        var correlationId = MartenTracing.GetCorrelationIdFromActivity();
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            activity?.SetTag("correlation.id", correlationId);
        }
        
        try
        {
            await session.SaveChangesAsync(cancellationToken);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error", true);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            throw;
        }
    }
}

