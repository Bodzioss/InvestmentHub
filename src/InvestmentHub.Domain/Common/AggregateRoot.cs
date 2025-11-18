using InvestmentHub.Domain.Common;

namespace InvestmentHub.Domain.Common;

/// <summary>
/// Base class for all aggregate roots in the domain.
/// Provides infrastructure for managing domain events and aggregate versioning.
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<DomainEvent> _uncommittedEvents = new();

    /// <summary>
    /// Gets the current version of the aggregate (for optimistic concurrency).
    /// </summary>
    public int Version { get; protected set; }

    /// <summary>
    /// Adds a domain event to the list of uncommitted events.
    /// </summary>
    /// <param name="event">The domain event to add</param>
    protected void AddDomainEvent(DomainEvent @event)
    {
        _uncommittedEvents.Add(@event);
    }

    /// <summary>
    /// Gets all uncommitted domain events.
    /// </summary>
    /// <returns>A read-only collection of uncommitted events</returns>
    public IReadOnlyCollection<DomainEvent> GetUncommittedEvents()
    {
        return _uncommittedEvents.AsReadOnly();
    }

    /// <summary>
    /// Clears all uncommitted events.
    /// Should be called after events have been persisted.
    /// </summary>
    public void ClearUncommittedEvents()
    {
        _uncommittedEvents.Clear();
    }
}

