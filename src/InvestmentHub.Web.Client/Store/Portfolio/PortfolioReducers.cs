using Fluxor;

namespace InvestmentHub.Web.Client.Store.Portfolio;

public static class PortfolioReducers
{
    [ReducerMethod]
    public static PortfolioState ReduceLoadPortfoliosAction(PortfolioState state, LoadPortfoliosAction action)
    {
        return state with { IsLoading = true, ErrorMessage = null };
    }

    [ReducerMethod]
    public static PortfolioState ReduceLoadPortfoliosSuccessAction(PortfolioState state, LoadPortfoliosSuccessAction action)
    {
        return state with 
        { 
            IsLoading = false, 
            Portfolios = action.Portfolios,
            ErrorMessage = null
        };
    }

    [ReducerMethod]
    public static PortfolioState ReduceLoadPortfoliosFailureAction(PortfolioState state, LoadPortfoliosFailureAction action)
    {
        return state with 
        { 
            IsLoading = false, 
            ErrorMessage = action.ErrorMessage 
        };
    }
}

