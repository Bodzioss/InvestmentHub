using System.Net.Http.Json;
using InvestmentHub.Contracts.Auth;

namespace InvestmentHub.Web.Client.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AuthResponse> Login(LoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AuthResponse>() ?? throw new InvalidOperationException("Empty response");
    }

    public async Task<AuthResponse> Register(RegisterRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AuthResponse>() ?? throw new InvalidOperationException("Empty response");
    }

    public async Task ChangePassword(ChangePasswordRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/change-password", request);
        response.EnsureSuccessStatusCode();
    }
}
