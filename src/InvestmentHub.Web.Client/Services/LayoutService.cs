using Blazored.LocalStorage;
using MudBlazor;

namespace InvestmentHub.Web.Client.Services;

public class LayoutService
{
    private readonly ILocalStorageService _localStorage;
    private MudTheme _currentTheme = new();
    private bool _isDarkMode;

    public LayoutService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public bool IsDarkMode => _isDarkMode;

    public MudTheme CurrentTheme => _currentTheme;

    public event EventHandler? MajorUpdateOccured;

    public async Task ToggleDarkMode()
    {
        _isDarkMode = !_isDarkMode;
        await _localStorage.SetItemAsync("darkMode", _isDarkMode);
        MajorUpdateOccured?.Invoke(this, EventArgs.Empty);
    }

    public async Task LoadUserPreferences()
    {
        _isDarkMode = await _localStorage.GetItemAsync<bool>("darkMode");
        MajorUpdateOccured?.Invoke(this, EventArgs.Empty);
    }

    public void SetBaseTheme(MudTheme theme)
    {
        _currentTheme = theme;
        MajorUpdateOccured?.Invoke(this, EventArgs.Empty);
    }
}

