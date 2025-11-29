using InvestmentHub.Domain.Common;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.DomainEvents;

/// <summary>
/// Marten-based domain event publisher that leverages the transactional outbox pattern.
/// This implementation does NOT publish events directly. Instead:
/// 1. Events are persisted to Marten's event store as part of the aggregate save transaction.
/// 2. Marten's Async Daemon processes events through MassTransitOutboxProjection.
/// 3. MassTransitOutboxProjection publishes messages to RabbitMQ.
/// This ensures atomicity between event persistence and message publishing.
/// </summary>
public class MartenOutboxDomainEventPublisher : IDomainEventPublisher
{
    private readonly ILogger<MartenOutboxDomainEventPublisher> _logger;

    public MartenOutboxDomainEventPublisher(ILogger<MartenOutboxDomainEventPublisher> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// No-op implementation. Events are persisted via Marten event store in command handlers.
    /// The MassTransitOutboxProjection (Async Daemon) handles the actual publishing to RabbitMQ.
    /// </summary>
    public Task PublishAsync<T>(T domainEvent) where T : DomainEvent
    {
        if (domainEvent == null)
            throw new ArgumentNullException(nameof(domainEvent));

        _logger.LogDebug("Domain event {EventType} will be published via Marten Outbox Pattern", 
            typeof(T).Name);

        // No-op: Marten event store + Async Daemon handles the publishing
        return Task.CompletedTask;
    }

    /// <summary>
    /// No-op implementation. Events are persisted via Marten event store in command handlers.
    /// The MassTransitOutboxProjection (Async Daemon) handles the actual publishing to RabbitMQ.
    /// </summary>
    public Task PublishAsync(IEnumerable<DomainEvent> domainEvents)
    {
        if (domainEvents == null)
            throw new ArgumentNullException(nameof(domainEvents));

        var eventsList = domainEvents.ToList();
        if (eventsList.Any())
        {
            _logger.LogDebug("Batch of {EventCount} domain events will be published via Marten Outbox Pattern", 
                eventsList.Count);
        }

        // No-op: Marten event store + Async Daemon handles the publishing
        return Task.CompletedTask;
    }
}
