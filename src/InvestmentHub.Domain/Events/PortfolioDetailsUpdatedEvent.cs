using InvestmentHub.Domain.Common;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Events;

public class PortfolioDetailsUpdatedEvent(PortfolioId portfolioId, string newName, string? newDescription, DateTime occurredOn) : DomainEvent(1)
{
    public PortfolioId PortfolioId { get; } = portfolioId;
    public string NewName { get; } = newName;
    public string? NewDescription { get; } = newDescription;
    public new DateTime OccurredOn { get; } = occurredOn;
}
