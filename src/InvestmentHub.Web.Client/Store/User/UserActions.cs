using InvestmentHub.Contracts;

namespace InvestmentHub.Web.Client.Store.User;

// Actions
public record LoadUsersAction;

public record LoadUsersSuccessAction(List<UserResponseDto> Users);

public record LoadUsersFailureAction(string ErrorMessage);

public record SelectUserAction(string UserId);

public record LoadSelectedUserFromStorageAction;

