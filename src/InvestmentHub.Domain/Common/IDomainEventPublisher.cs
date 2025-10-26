namespace InvestmentHub.Domain.Common;

/// <summary>
/// Interface for publishing domain events.
/// Implementations handle the distribution of domain events to registered subscribers.
/// </summary>
public interface IDomainEventPublisher
{
    /// <summary>
    /// Publishes a domain event to all registered subscribers.
    /// </summary>
    /// <typeparam name="T">The type of domain event</typeparam>
    /// <param name="domainEvent">The domain event to publish</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task PublishAsync<T>(T domainEvent) where T : DomainEvent;
    
    /// <summary>
    /// Publishes multiple domain events in batch.
    /// </summary>
    /// <param name="domainEvents">The collection of domain events to publish</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task PublishAsync(IEnumerable<DomainEvent> domainEvents);
}

/// <summary>
/// Interface for subscribing to specific domain events.
/// Implementations handle domain events of a specific type.
/// </summary>
/// <typeparam name="T">The type of domain event to handle</typeparam>
public interface IDomainEventSubscriber<in T> where T : DomainEvent
{
    /// <summary>
    /// Handles a domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to handle</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task HandleAsync(T domainEvent);
}

/// <summary>
/// Interface for registering domain event subscribers.
/// Used to configure which subscribers handle which events.
/// </summary>
public interface IDomainEventRegistry
{
    /// <summary>
    /// Registers a subscriber for a specific domain event type.
    /// </summary>
    /// <typeparam name="T">The type of domain event</typeparam>
    /// <param name="subscriber">The subscriber to register</param>
    void RegisterSubscriber<T>(IDomainEventSubscriber<T> subscriber) where T : DomainEvent;
    
    /// <summary>
    /// Gets all registered subscribers for a specific domain event type.
    /// </summary>
    /// <typeparam name="T">The type of domain event</typeparam>
    /// <returns>Collection of subscribers for the event type</returns>
    IEnumerable<IDomainEventSubscriber<T>> GetSubscribers<T>() where T : DomainEvent;
}
