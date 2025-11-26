using Fluxor;
using InvestmentHub.Web.Client.Services;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Web.Client.Store.Investment;

/// <summary>
/// Effects for Investment state
/// </summary>
public class InvestmentEffects
{
    private readonly IInvestmentsApi _investmentsApi;
    private readonly ILogger<InvestmentEffects> _logger;

    public InvestmentEffects(IInvestmentsApi investmentsApi, ILogger<InvestmentEffects> logger)
    {
        _investmentsApi = investmentsApi;
        _logger = logger;
    }

    [EffectMethod]
    public async Task HandleLoadInvestmentsAction(LoadInvestmentsAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Loading investments for portfolio {PortfolioId}", action.PortfolioId);

            var investments = await ApiErrorHandler.HandleApiCall(
                () => _investmentsApi.GetPortfolioInvestmentsAsync(action.PortfolioId)
            );

            dispatcher.Dispatch(new LoadInvestmentsSuccessAction(action.PortfolioId, investments));
            _logger.LogInformation("Loaded {Count} investments for portfolio {PortfolioId}", investments.Count, action.PortfolioId);
        }
        catch (InvestmentHubApiException ex)
        {
            _logger.LogError(ex, "Failed to load investments for portfolio {PortfolioId}", action.PortfolioId);
            dispatcher.Dispatch(new LoadInvestmentsFailureAction(ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleAddInvestmentAction(AddInvestmentAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Adding investment to portfolio {PortfolioId}", action.Request.PortfolioId);

            await ApiErrorHandler.HandleApiCall(
                () => _investmentsApi.AddInvestmentAsync(action.Request)
            );

            // Reload investments after adding
            dispatcher.Dispatch(new LoadInvestmentsAction(action.Request.PortfolioId));
            _logger.LogInformation("Investment added successfully");
        }
        catch (InvestmentHubApiException ex)
        {
            _logger.LogError(ex, "Failed to add investment");
            dispatcher.Dispatch(new LoadInvestmentsFailureAction(ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleUpdateInvestmentValueAction(UpdateInvestmentValueAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Updating investment value for {InvestmentId}", action.InvestmentId);

            var updatedInvestment = await ApiErrorHandler.HandleApiCall(
                () => _investmentsApi.UpdateInvestmentValueAsync(action.InvestmentId, action.Request)
            );

            // Reload investments after updating
            dispatcher.Dispatch(new LoadInvestmentsAction(updatedInvestment.PortfolioId));
            _logger.LogInformation("Investment value updated successfully");
        }
        catch (InvestmentHubApiException ex)
        {
            _logger.LogError(ex, "Failed to update investment value");
            dispatcher.Dispatch(new LoadInvestmentsFailureAction(ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleDeleteInvestmentAction(DeleteInvestmentAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Deleting investment {InvestmentId}", action.InvestmentId);

            await ApiErrorHandler.HandleApiCall(
                () => _investmentsApi.DeleteInvestmentAsync(action.InvestmentId)
            );

            _logger.LogInformation("Investment deleted successfully");
        }
        catch (InvestmentHubApiException ex)
        {
            _logger.LogError(ex, "Failed to delete investment");
            dispatcher.Dispatch(new LoadInvestmentsFailureAction(ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleSellInvestmentAction(SellInvestmentAction action, IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("Selling investment {InvestmentId}", action.InvestmentId);

            var updatedInvestment = await ApiErrorHandler.HandleApiCall(
                () => _investmentsApi.SellInvestmentAsync(action.InvestmentId, action.Request)
            );

            // Reload investments after selling
            dispatcher.Dispatch(new LoadInvestmentsAction(updatedInvestment.PortfolioId));
            _logger.LogInformation("Investment sold successfully");
        }
        catch (InvestmentHubApiException ex)
        {
            _logger.LogError(ex, "Failed to sell investment");
            dispatcher.Dispatch(new LoadInvestmentsFailureAction(ex.Message));
        }
    }
}

