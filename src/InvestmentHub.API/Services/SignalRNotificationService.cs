using InvestmentHub.API.Hubs;
using InvestmentHub.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace InvestmentHub.API.Services;

public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRNotificationService> _logger;

    public SignalRNotificationService(IHubContext<NotificationHub> hubContext, ILogger<SignalRNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyPortfolioUpdateAsync(Guid portfolioId, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group($"Portfolio_{portfolioId}")
                .SendAsync("ReceivePortfolioUpdate", portfolioId, message, cancellationToken);
            
            _logger.LogInformation("Sent portfolio update notification for Portfolio {PortfolioId}", portfolioId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send portfolio update notification for Portfolio {PortfolioId}", portfolioId);
        }
    }

    public async Task NotifyPriceUpdateAsync(string symbol, decimal price, CancellationToken cancellationToken = default)
    {
        try
        {
            // Broadcast to all connected clients (or specific groups if needed)
            await _hubContext.Clients.All.SendAsync("ReceivePriceUpdate", symbol, price, cancellationToken);
            
            _logger.LogInformation("Sent price update notification for {Symbol}: {Price}", symbol, price);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send price update notification for {Symbol}", symbol);
        }
    }
}
