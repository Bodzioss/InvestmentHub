using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using InvestmentHub.Web.Client;
using MudBlazor.Services;
using Blazored.LocalStorage;
using Fluxor;
using Refit;
using InvestmentHub.Web.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Get API base URL from configuration
// Aspire injects service discovery URLs via "services:api:https:0"
// Force HTTPS only (skip HTTP to avoid CORS issues with redirects)
var apiBaseUrl = builder.Configuration["services:api:https:0"] 
              ?? builder.Configuration["ApiSettings:BaseUrl"]
              ?? throw new InvalidOperationException("API URL is not configured");

Console.WriteLine($"[WebClient] API Base URL: {apiBaseUrl}"); // Debug log

// Configure HttpClient for API calls
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(apiBaseUrl) 
});

// MudBlazor services
builder.Services.AddMudServices();

// LocalStorage for user selection persistence
builder.Services.AddBlazoredLocalStorage();

// Fluxor state management
builder.Services.AddFluxor(options =>
{
    options.ScanAssemblies(typeof(Program).Assembly);
    // Note: Redux DevTools requires browser extension - optional for development
});

// Register Auth Header Handler
builder.Services.AddTransient<InvestmentHub.Web.Client.Auth.AuthHeaderHandler>();

// Refit API clients
builder.Services
    .AddRefitClient<InvestmentHub.Web.Client.Services.IUsersApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<InvestmentHub.Web.Client.Auth.AuthHeaderHandler>();

builder.Services
    .AddRefitClient<InvestmentHub.Web.Client.Services.IPortfoliosApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<InvestmentHub.Web.Client.Auth.AuthHeaderHandler>();

builder.Services
    .AddRefitClient<InvestmentHub.Web.Client.Services.IInvestmentsApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<InvestmentHub.Web.Client.Auth.AuthHeaderHandler>();

builder.Services
    .AddRefitClient<InvestmentHub.Web.Client.Services.IMarketDataApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<InvestmentHub.Web.Client.Auth.AuthHeaderHandler>();

builder.Services
    .AddRefitClient<InvestmentHub.Web.Client.Services.IInstrumentsApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<InvestmentHub.Web.Client.Auth.AuthHeaderHandler>();

builder.Services.AddScoped<LayoutService>();
builder.Services.AddScoped<SignalRService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, InvestmentHub.Web.Client.Auth.CustomAuthenticationStateProvider>();


await builder.Build().RunAsync();
