using Fluxor;

namespace InvestmentHub.Web.Client.Store.User;

public static class UserReducers
{
    [ReducerMethod]
    public static UserState ReduceLoadUsersAction(UserState state, LoadUsersAction action)
    {
        return state with { IsLoading = true };
    }

    [ReducerMethod]
    public static UserState ReduceLoadUsersSuccessAction(UserState state, LoadUsersSuccessAction action)
    {
        return state with 
        { 
            IsLoading = false, 
            Users = action.Users 
        };
    }

    [ReducerMethod]
    public static UserState ReduceLoadUsersFailureAction(UserState state, LoadUsersFailureAction action)
    {
        return state with { IsLoading = false };
    }

    [ReducerMethod]
    public static UserState ReduceSelectUserAction(UserState state, SelectUserAction action)
    {
        return state with { SelectedUserId = action.UserId };
    }
}

