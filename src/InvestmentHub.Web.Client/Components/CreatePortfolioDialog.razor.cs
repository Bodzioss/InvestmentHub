using InvestmentHub.Contracts;
using InvestmentHub.Web.Client.Store.Portfolio;
using InvestmentHub.Web.Client.Store.User;
using InvestmentHub.Web.Client.Services;
using Fluxor;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace InvestmentHub.Web.Client.Components;

public partial class CreatePortfolioDialog : ComponentBase
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = default!;

    [Inject]
    public IState<UserState> UserState { get; set; } = default!;

    [Inject]
    public IDispatcher Dispatcher { get; set; } = default!;

    [Inject]
    public IPortfoliosApi PortfoliosApi { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    private MudForm? _form;
    private bool _isValid;
    private string[] _errors = Array.Empty<string>();
    private string _portfolioName = string.Empty;
    private string? _description;
    private string _currency = "USD";
    private bool _isSubmitting = false;

    private async Task Submit()
    {
        if (UserState.Value.SelectedUserId == null)
        {
            Snackbar.Add("Please select a user first", Severity.Warning);
            return;
        }

        await _form!.Validate();
        if (!_isValid)
        {
            return;
        }

        _isSubmitting = true;
        StateHasChanged();

        try
        {
            var request = new CreatePortfolioRequest
            {
                Name = _portfolioName,
                Description = _description,
                OwnerId = UserState.Value.SelectedUserId
            };

            var portfolio = await ApiErrorHandler.HandleApiCall(() => PortfoliosApi.CreatePortfolioAsync(request));

            Snackbar.Add($"Portfolio '{portfolio.Name}' created successfully!", Severity.Success);

            // Reload portfolios for the current user
            Dispatcher.Dispatch(new LoadPortfoliosAction(UserState.Value.SelectedUserId));

            MudDialog.Close(DialogResult.Ok(portfolio));
        }
        catch (InvestmentHubApiException ex)
        {
            Snackbar.Add(ex.GetUserFriendlyMessage(), Severity.Error);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to create portfolio: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
            StateHasChanged();
        }
    }

    private void Cancel()
    {
        MudDialog.Cancel();
    }
}
