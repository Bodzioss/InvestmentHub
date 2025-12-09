using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Blazored.LocalStorage;

namespace InvestmentHub.Web.Client.Services;

public class SignalRService : IAsyncDisposable
{
    private readonly NavigationManager _navigationManager;
    private readonly ILocalStorageService _localStorage;
    private HubConnection? _hubConnection;
    private bool _started = false;

    public event Action<Guid, string>? OnPortfolioUpdated;
    public event Action<string, decimal>? OnPriceUpdated;

    public SignalRService(NavigationManager navigationManager, ILocalStorageService localStorage)
    {
        _navigationManager = navigationManager;
        _localStorage = localStorage;
    }

    public async Task StartConnectionAsync()
    {
        if (_started && _hubConnection?.State == HubConnectionState.Connected)
        {
            return;
        }

        var token = await _localStorage.GetItemAsync<string>("authToken");

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("/hubs/notifications"), options => 
            {
                options.AccessTokenProvider = () => Task.FromResult(token);
            })
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<Guid, string>("ReceivePortfolioUpdate", (portfolioId, message) =>
        {
            OnPortfolioUpdated?.Invoke(portfolioId, message);
        });

        _hubConnection.On<string, decimal>("ReceivePriceUpdate", (symbol, price) =>
        {
            OnPriceUpdated?.Invoke(symbol, price);
        });

        await _hubConnection.StartAsync();
        _started = true;
    }

    public async Task JoinPortfolioGroupAsync(Guid portfolioId)
    {
        if (_hubConnection is not null && _hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.SendAsync("JoinPortfolioGroup", portfolioId);
        }
    }

    public async Task LeavePortfolioGroupAsync(Guid portfolioId)
    {
        if (_hubConnection is not null && _hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.SendAsync("LeavePortfolioGroup", portfolioId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
