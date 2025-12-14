using InvestmentHub.Domain.Common;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Events;

public class PortfolioDetailsUpdatedEvent : DomainEvent
{
    public PortfolioId PortfolioId { get; }
    public string NewName { get; }
    public string? NewDescription { get; }
    public DateTime OccurredOn { get; }

    public PortfolioDetailsUpdatedEvent(
        PortfolioId portfolioId,
        string newName,
        string? newDescription,
        DateTime occurredOn) : base(1)
    {
        PortfolioId = portfolioId;
        NewName = newName;
        NewDescription = newDescription;
        OccurredOn = occurredOn;
    }
}
