using Fluxor;

namespace InvestmentHub.Web.Client.Store.Investment;

/// <summary>
/// Reducers for Investment state
/// </summary>
public static class InvestmentReducers
{
    [ReducerMethod]
    public static InvestmentState ReduceLoadInvestmentsAction(InvestmentState state, LoadInvestmentsAction action)
    {
        return state with
        {
            IsLoading = true,
            ErrorMessage = null,
            SelectedPortfolioId = action.PortfolioId
        };
    }

    [ReducerMethod]
    public static InvestmentState ReduceLoadInvestmentsSuccessAction(InvestmentState state, LoadInvestmentsSuccessAction action)
    {
        return state with
        {
            Investments = action.Investments,
            IsLoading = false,
            ErrorMessage = null,
            SelectedPortfolioId = action.PortfolioId
        };
    }

    [ReducerMethod]
    public static InvestmentState ReduceLoadInvestmentsFailureAction(InvestmentState state, LoadInvestmentsFailureAction action)
    {
        return state with
        {
            IsLoading = false,
            ErrorMessage = action.ErrorMessage,
            Investments = new()
        };
    }
}

