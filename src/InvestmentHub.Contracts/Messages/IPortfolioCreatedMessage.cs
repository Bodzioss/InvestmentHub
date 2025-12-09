namespace InvestmentHub.Contracts.Messages;

/// <summary>
/// Message contract for when a portfolio is created.
/// </summary>
public interface IPortfolioCreatedMessage
{
    Guid PortfolioId { get; }
    Guid OwnerId { get; }
    string Name { get; }
    string? Description { get; }
    DateTime CreatedAt { get; }
}
