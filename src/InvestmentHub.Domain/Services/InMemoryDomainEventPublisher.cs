using InvestmentHub.Domain.Common;

namespace InvestmentHub.Domain.Services;

/// <summary>
/// Simple in-memory implementation of domain event publisher and registry.
/// This implementation handles event publishing synchronously for demonstration purposes.
/// In a production environment, you would typically use a more sophisticated event bus.
/// </summary>
public class InMemoryDomainEventPublisher : IDomainEventPublisher, IDomainEventRegistry
{
    private readonly Dictionary<Type, List<object>> _subscribers = new();
    
    /// <summary>
    /// Publishes a domain event to all registered subscribers.
    /// </summary>
    /// <typeparam name="T">The type of domain event</typeparam>
    /// <param name="domainEvent">The domain event to publish</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task PublishAsync<T>(T domainEvent) where T : DomainEvent
    {
        if (domainEvent == null)
            throw new ArgumentNullException(nameof(domainEvent));
        
        var eventType = typeof(T);
        
        if (_subscribers.TryGetValue(eventType, out var subscribers))
        {
            var tasks = subscribers
                .Cast<IDomainEventSubscriber<T>>()
                .Select(subscriber => subscriber.HandleAsync(domainEvent));
            
            await Task.WhenAll(tasks);
        }
    }
    
    /// <summary>
    /// Publishes multiple domain events in batch.
    /// </summary>
    /// <param name="domainEvents">The collection of domain events to publish</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task PublishAsync(IEnumerable<DomainEvent> domainEvents)
    {
        if (domainEvents == null)
            throw new ArgumentNullException(nameof(domainEvents));
        
        var tasks = domainEvents.Select(PublishEventAsync);
        await Task.WhenAll(tasks);
    }
    
    /// <summary>
    /// Registers a subscriber for a specific domain event type.
    /// </summary>
    /// <typeparam name="T">The type of domain event</typeparam>
    /// <param name="subscriber">The subscriber to register</param>
    public void RegisterSubscriber<T>(IDomainEventSubscriber<T> subscriber) where T : DomainEvent
    {
        if (subscriber == null)
            throw new ArgumentNullException(nameof(subscriber));
        
        var eventType = typeof(T);
        
        if (!_subscribers.ContainsKey(eventType))
        {
            _subscribers[eventType] = new List<object>();
        }
        
        _subscribers[eventType].Add(subscriber);
    }
    
    /// <summary>
    /// Gets all registered subscribers for a specific domain event type.
    /// </summary>
    /// <typeparam name="T">The type of domain event</typeparam>
    /// <returns>Collection of subscribers for the event type</returns>
    public IEnumerable<IDomainEventSubscriber<T>> GetSubscribers<T>() where T : DomainEvent
    {
        var eventType = typeof(T);
        
        if (_subscribers.TryGetValue(eventType, out var subscribers))
        {
            return subscribers.Cast<IDomainEventSubscriber<T>>();
        }
        
        return Enumerable.Empty<IDomainEventSubscriber<T>>();
    }
    
    /// <summary>
    /// Publishes a single domain event using reflection to determine the type.
    /// </summary>
    /// <param name="domainEvent">The domain event to publish</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task PublishEventAsync(DomainEvent domainEvent)
    {
        var eventType = domainEvent.GetType();
        
        if (_subscribers.TryGetValue(eventType, out var subscribers))
        {
            var tasks = subscribers.Select(subscriber =>
            {
                var handleMethod = subscriber.GetType()
                    .GetMethod("HandleAsync", new[] { eventType });
                
                if (handleMethod != null)
                {
                    var result = handleMethod.Invoke(subscriber, new object[] { domainEvent });
                    return result as Task;
                }
                
                return Task.CompletedTask;
            })
            .Where(t => t != null)
            .Cast<Task>();
            
            await Task.WhenAll(tasks);
        }
    }
}
