namespace InvestmentHub.Domain.Common;

/// <summary>
/// Base class for all domain events.
/// Domain events represent something important that happened in the domain.
/// They are used to decouple domain logic and enable event-driven architecture.
/// </summary>
public abstract class DomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the event.
    /// </summary>
    public Guid EventId { get; }
    
    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public DateTime OccurredOn { get; }
    
    /// <summary>
    /// Gets the version of the event for schema evolution.
    /// </summary>
    public int Version { get; }
    
    /// <summary>
    /// Initializes a new instance of the DomainEvent class.
    /// </summary>
    /// <param name="version">Version of the event schema (default: 1)</param>
    protected DomainEvent(int version = 1)
    {
        EventId = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
        Version = version;
    }
    
    /// <summary>
    /// Gets the type name of the event for serialization and routing.
    /// </summary>
    /// <returns>The full type name of the event</returns>
    public string GetEventType() => GetType().FullName ?? GetType().Name;
}
