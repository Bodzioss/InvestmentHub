namespace InvestmentHub.Domain.Interfaces;

public interface INotificationService
{
    Task NotifyPortfolioUpdateAsync(Guid portfolioId, string message, CancellationToken cancellationToken = default);
    Task NotifyPriceUpdateAsync(string symbol, decimal price, CancellationToken cancellationToken = default);
}
