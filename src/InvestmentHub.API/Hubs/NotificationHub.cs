using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace InvestmentHub.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Optionally add user to a personal group based on User ID
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinPortfolioGroup(Guid portfolioId)
    {
        // In a real app, verify access to the portfolio here
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Portfolio_{portfolioId}");
    }

    public async Task LeavePortfolioGroup(Guid portfolioId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Portfolio_{portfolioId}");
    }
}
