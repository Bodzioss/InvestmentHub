using Blazored.LocalStorage;
using Fluxor;
using InvestmentHub.Web.Client.Services;

namespace InvestmentHub.Web.Client.Store.User;

public class UserEffects
{
    private readonly IUsersApi _usersApi;
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<UserEffects> _logger;

    public UserEffects(
        IUsersApi usersApi, 
        ILocalStorageService localStorage,
        ILogger<UserEffects> logger)
    {
        _usersApi = usersApi;
        _localStorage = localStorage;
        _logger = logger;
    }

    [EffectMethod]
    public async Task HandleLoadUsersAction(LoadUsersAction action, IDispatcher dispatcher)
    {
        try
        {
            var users = await ApiErrorHandler.HandleApiCall(() => _usersApi.GetUsersAsync());
            dispatcher.Dispatch(new LoadUsersSuccessAction(users));
            
            // After loading users, try to restore selection
            dispatcher.Dispatch(new LoadSelectedUserFromStorageAction());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load users");
            dispatcher.Dispatch(new LoadUsersFailureAction(ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleSelectUserAction(SelectUserAction action, IDispatcher dispatcher)
    {
        await _localStorage.SetItemAsync("selectedUserId", action.UserId);
    }

    [EffectMethod]
    public async Task HandleLoadSelectedUserFromStorageAction(LoadSelectedUserFromStorageAction action, IDispatcher dispatcher)
    {
        var userId = await _localStorage.GetItemAsync<string>("selectedUserId");
        if (!string.IsNullOrEmpty(userId))
        {
            dispatcher.Dispatch(new SelectUserAction(userId));
        }
    }
}

