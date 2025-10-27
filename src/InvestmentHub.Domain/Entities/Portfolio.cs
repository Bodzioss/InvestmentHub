using InvestmentHub.Domain.Common;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Events;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Entities;

/// <summary>
/// Represents a collection of investments owned by a user.
/// Aggregate root that manages investment collection and enforces business rules.
/// </summary>
public class Portfolio
{
    private readonly List<Investment> _investments = new();
    private readonly List<DomainEvent> _domainEvents = new();
    
    /// <summary>
    /// Gets the unique identifier of the portfolio.
    /// </summary>
    public PortfolioId Id { get; private set; }
    
    /// <summary>
    /// Gets the name of the portfolio.
    /// </summary>
    public string Name { get; private set; }
    
    /// <summary>
    /// Gets the description of the portfolio.
    /// </summary>
    public string Description { get; private set; }
    
    /// <summary>
    /// Gets the unique identifier of the portfolio owner.
    /// </summary>
    public UserId OwnerId { get; private set; }
    
    /// <summary>
    /// Gets the date when the portfolio was created.
    /// </summary>
    public DateTime CreatedDate { get; private set; }
    
    /// <summary>
    /// Gets the date when the portfolio was last updated.
    /// </summary>
    public DateTime LastUpdated { get; private set; }
    
    /// <summary>
    /// Gets the current status of the portfolio.
    /// </summary>
    public PortfolioStatus Status { get; private set; }
    
    /// <summary>
    /// Gets a read-only collection of investments in the portfolio.
    /// </summary>
    public IReadOnlyCollection<Investment> Investments => _investments.AsReadOnly();
    
    /// <summary>
    /// Gets a read-only collection of domain events raised by this aggregate.
    /// </summary>
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    /// <summary>
    /// Private parameterless constructor for EF Core.
    /// </summary>
    private Portfolio()
    {
        // EF Core requires a parameterless constructor
        // Values will be set through properties
    }
    
    /// <summary>
    /// Initializes a new instance of the Portfolio class.
    /// </summary>
    /// <param name="id">Unique identifier for the portfolio</param>
    /// <param name="name">Name of the portfolio</param>
    /// <param name="description">Description of the portfolio</param>
    /// <param name="ownerId">Unique identifier of the portfolio owner</param>
    /// <exception cref="ArgumentException">Thrown when name is null or empty</exception>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    public Portfolio(PortfolioId id, string name, string description, UserId ownerId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Portfolio name cannot be null or empty", nameof(name));
        
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name;
        Description = description ?? string.Empty;
        OwnerId = ownerId ?? throw new ArgumentNullException(nameof(ownerId));
        CreatedDate = DateTime.UtcNow;
        LastUpdated = DateTime.UtcNow;
        Status = PortfolioStatus.Active;
    }
    
    /// <summary>
    /// Updates the portfolio name and description.
    /// </summary>
    /// <param name="name">New name for the portfolio</param>
    /// <param name="description">New description for the portfolio</param>
    /// <exception cref="InvalidOperationException">Thrown when portfolio is not active</exception>
    /// <exception cref="ArgumentException">Thrown when name is null or empty</exception>
    public void UpdateDetails(string name, string description)
    {
        if (Status != PortfolioStatus.Active)
            throw new InvalidOperationException("Cannot update inactive portfolio");
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Portfolio name cannot be null or empty", nameof(name));
        
        Name = name;
        Description = description ?? string.Empty;
        LastUpdated = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Adds a new investment to the portfolio.
    /// </summary>
    /// <param name="investment">The investment to add</param>
    /// <exception cref="InvalidOperationException">Thrown when portfolio is not active or investment already exists</exception>
    /// <exception cref="ArgumentNullException">Thrown when investment is null</exception>
    public void AddInvestment(Investment investment)
    {
        if (Status != PortfolioStatus.Active)
            throw new InvalidOperationException("Cannot add investments to inactive portfolio");
        
        if (investment == null)
            throw new ArgumentNullException(nameof(investment));
        
        // Check if investment with same symbol already exists
        if (_investments.Any(i => i.Symbol == investment.Symbol && i.Status == InvestmentStatus.Active))
            throw new InvalidOperationException($"Investment with symbol {investment.Symbol.Ticker} already exists in portfolio");
        
        _investments.Add(investment);
        LastUpdated = DateTime.UtcNow;
        
        // Raise domain event
        var investmentAddedEvent = InvestmentAddedEvent.FromInvestment(investment, Id, OwnerId);
        _domainEvents.Add(investmentAddedEvent);
    }
    
    /// <summary>
    /// Removes an investment from the portfolio.
    /// </summary>
    /// <param name="investmentId">The ID of the investment to remove</param>
    /// <exception cref="InvalidOperationException">Thrown when portfolio is not active or investment not found</exception>
    public void RemoveInvestment(InvestmentId investmentId)
    {
        if (Status != PortfolioStatus.Active)
            throw new InvalidOperationException("Cannot remove investments from inactive portfolio");
        
        var investment = _investments.FirstOrDefault(i => i.Id == investmentId);
        if (investment == null)
            throw new InvalidOperationException($"Investment with ID {investmentId.Value} not found in portfolio");
        
        _investments.Remove(investment);
        LastUpdated = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Gets an investment by its ID.
    /// </summary>
    /// <param name="investmentId">The ID of the investment to find</param>
    /// <returns>The investment if found, null otherwise</returns>
    public Investment? GetInvestment(InvestmentId investmentId)
    {
        return _investments.FirstOrDefault(i => i.Id == investmentId);
    }
    
    /// <summary>
    /// Gets an investment by its symbol.
    /// </summary>
    /// <param name="symbol">The symbol of the investment to find</param>
    /// <returns>The investment if found, null otherwise</returns>
    public Investment? GetInvestmentBySymbol(Symbol symbol)
    {
        return _investments.FirstOrDefault(i => i.Symbol == symbol && i.Status == InvestmentStatus.Active);
    }
    
    /// <summary>
    /// Calculates the total current value of all active investments in the portfolio.
    /// </summary>
    /// <returns>Total value as Money</returns>
    public Money GetTotalValue()
    {
        var activeInvestments = _investments.Where(i => i.Status == InvestmentStatus.Active);
        
        if (!activeInvestments.Any())
            return Money.Zero(Currency.USD); // Default currency, could be made configurable
        
        var firstInvestment = activeInvestments.First();
        var totalValue = firstInvestment.CurrentValue;
        
        foreach (var investment in activeInvestments.Skip(1))
        {
            if (totalValue.Currency == investment.CurrentValue.Currency)
            {
                totalValue = totalValue.Add(investment.CurrentValue);
            }
            else
            {
                // Handle multi-currency portfolios - for now, throw exception
                throw new InvalidOperationException("Portfolio contains investments in different currencies. Multi-currency support not implemented.");
            }
        }
        
        return totalValue;
    }
    
    /// <summary>
    /// Calculates the total cost of all investments in the portfolio.
    /// </summary>
    /// <returns>Total cost as Money</returns>
    public Money GetTotalCost()
    {
        var activeInvestments = _investments.Where(i => i.Status == InvestmentStatus.Active);
        
        if (!activeInvestments.Any())
            return Money.Zero(Currency.USD); // Default currency, could be made configurable
        
        var firstInvestment = activeInvestments.First();
        var totalCost = firstInvestment.GetTotalCost();
        
        foreach (var investment in activeInvestments.Skip(1))
        {
            if (totalCost.Currency == investment.GetTotalCost().Currency)
            {
                totalCost = totalCost.Add(investment.GetTotalCost());
            }
            else
            {
                // Handle multi-currency portfolios - for now, throw exception
                throw new InvalidOperationException("Portfolio contains investments in different currencies. Multi-currency support not implemented.");
            }
        }
        
        return totalCost;
    }
    
    /// <summary>
    /// Calculates the total unrealized gain or loss for the portfolio.
    /// </summary>
    /// <returns>Total gain/loss as Money</returns>
    public Money GetTotalGainLoss() => GetTotalValue().Subtract(GetTotalCost());
    
    /// <summary>
    /// Calculates the percentage gain or loss for the portfolio.
    /// </summary>
    /// <returns>Percentage gain/loss as decimal (e.g., 0.15 for 15% gain)</returns>
    public decimal GetPercentageGainLoss()
    {
        var totalCost = GetTotalCost();
        if (totalCost.Amount == 0) return 0;
        
        return (GetTotalValue().Amount - totalCost.Amount) / totalCost.Amount;
    }
    
    /// <summary>
    /// Gets the number of active investments in the portfolio.
    /// </summary>
    /// <returns>Count of active investments</returns>
    public int GetActiveInvestmentCount() => _investments.Count(i => i.Status == InvestmentStatus.Active);
    
    /// <summary>
    /// Archives the portfolio, making it read-only.
    /// </summary>
    public void Archive()
    {
        Status = PortfolioStatus.Archived;
        LastUpdated = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Suspends the portfolio from modifications.
    /// </summary>
    public void Suspend()
    {
        Status = PortfolioStatus.Suspended;
        LastUpdated = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Reactivates the portfolio if it was suspended.
    /// </summary>
    public void Reactivate()
    {
        if (Status == PortfolioStatus.Suspended)
        {
            Status = PortfolioStatus.Active;
            LastUpdated = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Clears all domain events from this aggregate.
    /// This should be called after events have been processed by the event publisher.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
    
    /// <summary>
    /// Returns a string representation of the portfolio.
    /// </summary>
    /// <returns>Formatted string with portfolio details</returns>
    public override string ToString() => 
        $"Portfolio '{Name}' ({Id.Value}): {GetActiveInvestmentCount()} investments, Total Value: {GetTotalValue()}, Status: {Status}";
}
