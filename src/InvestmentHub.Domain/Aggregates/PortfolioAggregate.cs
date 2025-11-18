using InvestmentHub.Domain.Common;
using InvestmentHub.Domain.Events;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Aggregates;

/// <summary>
/// Aggregate root for Portfolio using Event Sourcing pattern.
/// Manages portfolio state through events and provides business logic.
/// </summary>
public class PortfolioAggregate : AggregateRoot
{
    // State - rebuilt from events
    /// <summary>Gets the unique identifier of the portfolio.</summary>
    public Guid Id { get; private set; }
    
    /// <summary>Gets the portfolio identifier value object.</summary>
    public PortfolioId PortfolioId { get; private set; } = null!;
    
    /// <summary>Gets the owner user identifier.</summary>
    public UserId OwnerId { get; private set; } = null!;
    
    /// <summary>Gets the portfolio name.</summary>
    public string Name { get; private set; } = string.Empty;
    
    /// <summary>Gets the portfolio description.</summary>
    public string? Description { get; private set; }
    
    /// <summary>Gets a value indicating whether the portfolio is closed.</summary>
    public bool IsClosed { get; private set; }
    
    /// <summary>Gets the date and time when the portfolio was created.</summary>
    public DateTime CreatedAt { get; private set; }
    
    /// <summary>Gets the date and time when the portfolio was closed.</summary>
    public DateTime? ClosedAt { get; private set; }
    
    /// <summary>Gets the reason for closing the portfolio.</summary>
    public string? CloseReason { get; private set; }

    // Marten requires a parameterless constructor for deserialization
    /// <summary>
    /// Initializes a new instance of the PortfolioAggregate class.
    /// Used by Marten for event stream replay.
    /// </summary>
    public PortfolioAggregate()
    {
    }

    // Business methods - generate events

    /// <summary>
    /// Creates a new portfolio aggregate.
    /// This is a factory method that generates the initial PortfolioCreatedEvent.
    /// </summary>
    /// <param name="portfolioId">The unique identifier for the portfolio</param>
    /// <param name="ownerId">The owner user identifier</param>
    /// <param name="name">The portfolio name</param>
    /// <param name="description">Optional portfolio description</param>
    /// <returns>A new PortfolioAggregate instance with PortfolioCreatedEvent</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    /// <exception cref="ArgumentException">Thrown when name is empty</exception>
    public static PortfolioAggregate Create(
        PortfolioId portfolioId,
        UserId ownerId,
        string name,
        string? description = null)
    {
        if (portfolioId == null)
            throw new ArgumentNullException(nameof(portfolioId));
        if (ownerId == null)
            throw new ArgumentNullException(nameof(ownerId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Portfolio name cannot be empty", nameof(name));

        var aggregate = new PortfolioAggregate();
        var @event = new PortfolioCreatedEvent(
            portfolioId,
            ownerId,
            name,
            description,
            DateTime.UtcNow);

        aggregate.Apply(@event);
        aggregate.AddDomainEvent(@event);
        return aggregate;
    }

    /// <summary>
    /// Renames the portfolio.
    /// Generates a PortfolioRenamedEvent if the new name is different.
    /// </summary>
    /// <param name="newName">The new name for the portfolio</param>
    /// <returns>The PortfolioRenamedEvent</returns>
    /// <exception cref="ArgumentException">Thrown when name is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when portfolio is closed</exception>
    public PortfolioRenamedEvent Rename(string newName)
    {
        if (IsClosed)
            throw new InvalidOperationException("Cannot rename a closed portfolio");

        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("New name cannot be empty", nameof(newName));

        if (Name.Equals(newName, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("New name must be different from current name", nameof(newName));

        var @event = new PortfolioRenamedEvent(
            PortfolioId,
            Name,
            newName,
            DateTime.UtcNow);

        Apply(@event);
        AddDomainEvent(@event);
        return @event;
    }

    /// <summary>
    /// Closes the portfolio.
    /// Generates a PortfolioClosedEvent.
    /// </summary>
    /// <param name="reason">Optional reason for closing</param>
    /// <param name="closedBy">The user who is closing the portfolio</param>
    /// <returns>The PortfolioClosedEvent</returns>
    /// <exception cref="InvalidOperationException">Thrown when portfolio is already closed</exception>
    /// <exception cref="ArgumentNullException">Thrown when closedBy is null</exception>
    public PortfolioClosedEvent Close(string? reason, UserId closedBy)
    {
        if (IsClosed)
            throw new InvalidOperationException("Portfolio is already closed");

        if (closedBy == null)
            throw new ArgumentNullException(nameof(closedBy));

        var @event = new PortfolioClosedEvent(
            PortfolioId,
            Name,
            reason,
            DateTime.UtcNow,
            closedBy);

        Apply(@event);
        AddDomainEvent(@event);
        return @event;
    }

    // Apply methods - update state from events (Event Sourcing pattern)

    /// <summary>
    /// Applies a PortfolioCreatedEvent to set the initial state.
    /// This method is called during event stream replay.
    /// </summary>
    /// <param name="event">The PortfolioCreatedEvent to apply</param>
    public void Apply(PortfolioCreatedEvent @event)
    {
        Id = @event.PortfolioId.Value;
        PortfolioId = @event.PortfolioId;
        OwnerId = @event.OwnerId;
        Name = @event.Name;
        Description = @event.Description;
        IsClosed = false;
        CreatedAt = @event.CreatedAt;
        Version++;
    }

    /// <summary>
    /// Applies a PortfolioRenamedEvent to update the name.
    /// This method is called during event stream replay.
    /// </summary>
    /// <param name="event">The PortfolioRenamedEvent to apply</param>
    public void Apply(PortfolioRenamedEvent @event)
    {
        Name = @event.NewName;
        Version++;
    }

    /// <summary>
    /// Applies a PortfolioClosedEvent to close the portfolio.
    /// This method is called during event stream replay.
    /// </summary>
    /// <param name="event">The PortfolioClosedEvent to apply</param>
    public void Apply(PortfolioClosedEvent @event)
    {
        IsClosed = true;
        ClosedAt = @event.ClosedAt;
        CloseReason = @event.Reason;
        Version++;
    }

    /// <summary>
    /// Returns a string representation of the aggregate.
    /// </summary>
    public override string ToString() =>
        $"Portfolio '{Name}' (ID: {Id}, Owner: {OwnerId.Value}, Closed: {IsClosed}, Version: {Version})";
}

