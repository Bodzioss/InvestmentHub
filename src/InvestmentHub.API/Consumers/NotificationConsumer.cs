using InvestmentHub.Contracts.Messages;
using InvestmentHub.Domain.Interfaces;
using MassTransit;

namespace InvestmentHub.API.Consumers;

public class NotificationConsumer : IConsumer<PortfolioUpdatedMessage>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationConsumer> _logger;

    public NotificationConsumer(INotificationService notificationService, ILogger<NotificationConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PortfolioUpdatedMessage> context)
    {
        _logger.LogInformation("Received PortfolioUpdatedMessage for Portfolio {PortfolioId}", context.Message.PortfolioId);
        
        await _notificationService.NotifyPortfolioUpdateAsync(
            context.Message.PortfolioId, 
            context.Message.Message, 
            context.CancellationToken);
    }
}
