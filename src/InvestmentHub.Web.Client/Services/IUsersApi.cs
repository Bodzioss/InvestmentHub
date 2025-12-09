using InvestmentHub.Contracts;
using Refit;

namespace InvestmentHub.Web.Client.Services;

/// <summary>
/// Refit API client for Users endpoints
/// </summary>
public interface IUsersApi
{
    /// <summary>
    /// Get all users
    /// </summary>
    [Get("/api/users")]
    Task<List<UserResponseDto>> GetUsersAsync();

    /// <summary>
    /// Get user by ID
    /// </summary>
    [Get("/api/users/{id}")]
    Task<UserResponseDto> GetUserByIdAsync(string id);
    /// <summary>
    /// Update user
    /// </summary>
    [Put("/api/users/{id}")]
    Task UpdateUserAsync(string id, [Body] InvestmentHub.Contracts.Users.UpdateUserRequest request);
}

