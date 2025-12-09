using InvestmentHub.Contracts.Auth;

namespace InvestmentHub.Web.Client.Services;

public interface IAuthService
{
    Task<AuthResponse> Login(LoginRequest request);
    Task<AuthResponse> Register(RegisterRequest request);
    Task ChangePassword(ChangePasswordRequest request);
}
