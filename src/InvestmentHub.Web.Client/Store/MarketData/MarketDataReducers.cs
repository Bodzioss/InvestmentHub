using Fluxor;
using InvestmentHub.Contracts;

namespace InvestmentHub.Web.Client.Store.MarketData;

public static class MarketDataReducers
{
    [ReducerMethod]
    public static MarketDataState ReduceFetchPrice(MarketDataState state, FetchPriceAction action) =>
        new(state.Prices, state.SelectedHistory, true, null);

    [ReducerMethod]
    public static MarketDataState ReduceFetchPriceSuccess(MarketDataState state, FetchPriceSuccessAction action)
    {
        var newPrices = new Dictionary<string, MarketPriceDto>(state.Prices)
        {
            [action.Price.Symbol] = action.Price
        };
        return new(newPrices, state.SelectedHistory, false, null);
    }

    [ReducerMethod]
    public static MarketDataState ReduceFetchPriceFailure(MarketDataState state, FetchPriceFailureAction action) =>
        new(state.Prices, state.SelectedHistory, false, action.ErrorMessage);

    [ReducerMethod]
    public static MarketDataState ReduceFetchHistory(MarketDataState state, FetchHistoryAction action) =>
        new(state.Prices, new List<MarketPriceDto>(), true, null);

    [ReducerMethod]
    public static MarketDataState ReduceFetchHistorySuccess(MarketDataState state, FetchHistorySuccessAction action) =>
        new(state.Prices, action.History.ToList(), false, null);

    [ReducerMethod]
    public static MarketDataState ReduceFetchHistoryFailure(MarketDataState state, FetchHistoryFailureAction action) =>
        new(state.Prices, state.SelectedHistory, false, action.ErrorMessage);
}
