using Fluxor;
using InvestmentHub.Web.Client.Services;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Web.Client.Store.MarketData;

public class MarketDataEffects
{
    private readonly IMarketDataApi _api;
    private readonly ILogger<MarketDataEffects> _logger;

    public MarketDataEffects(IMarketDataApi api, ILogger<MarketDataEffects> logger)
    {
        _api = api;
        _logger = logger;
    }

    [EffectMethod]
    public async Task HandleFetchPrice(FetchPriceAction action, IDispatcher dispatcher)
    {
        try
        {
            var price = await _api.GetPriceAsync(action.Symbol);
            dispatcher.Dispatch(new FetchPriceSuccessAction(price));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching price for {Symbol}", action.Symbol);
            dispatcher.Dispatch(new FetchPriceFailureAction(ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleFetchHistory(FetchHistoryAction action, IDispatcher dispatcher)
    {
        try
        {
            var history = await _api.GetHistoryAsync(action.Symbol);
            dispatcher.Dispatch(new FetchHistorySuccessAction(history));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching history for {Symbol}", action.Symbol);
            dispatcher.Dispatch(new FetchHistoryFailureAction(ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleImportHistory(ImportHistoryAction action, IDispatcher dispatcher)
    {
        try
        {
            await _api.ImportHistoryAsync(action.Symbol);
            dispatcher.Dispatch(new ImportHistorySuccessAction(action.Symbol));
            
            // Optionally trigger a history fetch after a delay or let user do it
            // For now just log success
            _logger.LogInformation("Import started for {Symbol}", action.Symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting import for {Symbol}", action.Symbol);
        }
    }
}
