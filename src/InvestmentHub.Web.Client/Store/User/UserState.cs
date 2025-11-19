using Fluxor;
using InvestmentHub.Contracts;

namespace InvestmentHub.Web.Client.Store.User;

[FeatureState]
public record UserState
{
    public bool IsLoading { get; init; }
    public List<UserResponseDto> Users { get; init; } = new();
    public string? SelectedUserId { get; init; }
    public UserResponseDto? SelectedUser => Users.FirstOrDefault(u => u.Id == SelectedUserId);

    private UserState() { } // Required for creating initial state

    public UserState(bool isLoading, List<UserResponseDto> users, string? selectedUserId)
    {
        IsLoading = isLoading;
        Users = users;
        SelectedUserId = selectedUserId;
    }
}

