using Fluxor;
using InvestmentHub.Web.Client.Services;
using InvestmentHub.Web.Client.Store.User;

namespace InvestmentHub.Web.Client.Store.Portfolio;

public class PortfolioEffects
{
    private readonly IPortfoliosApi _portfoliosApi;
    private readonly ILogger<PortfolioEffects> _logger;

    public PortfolioEffects(
        IPortfoliosApi portfoliosApi,
        ILogger<PortfolioEffects> logger)
    {
        _portfoliosApi = portfoliosApi;
        _logger = logger;
    }

    [EffectMethod]
    public async Task HandleLoadPortfoliosAction(LoadPortfoliosAction action, IDispatcher dispatcher)
    {
        try
        {
            var portfolios = await ApiErrorHandler.HandleApiCall(() => _portfoliosApi.GetUserPortfoliosAsync(action.UserId));
            dispatcher.Dispatch(new LoadPortfoliosSuccessAction(portfolios));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load portfolios");
            dispatcher.Dispatch(new LoadPortfoliosFailureAction(ex.Message));
        }
    }

    // Automatically load portfolios when a user is selected
    [EffectMethod]
    public Task HandleSelectUserAction(SelectUserAction action, IDispatcher dispatcher)
    {
        if (!string.IsNullOrEmpty(action.UserId))
        {
            dispatcher.Dispatch(new LoadPortfoliosAction(action.UserId));
        }
        else
        {
            // Clear portfolios if no user selected
            dispatcher.Dispatch(new LoadPortfoliosSuccessAction(new()));
        }
        return Task.CompletedTask;
    }
}

