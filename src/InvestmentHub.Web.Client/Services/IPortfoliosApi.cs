using InvestmentHub.Contracts;
using Refit;

namespace InvestmentHub.Web.Client.Services;

/// <summary>
/// Refit API client for Portfolios endpoints
/// </summary>
public interface IPortfoliosApi
{
    /// <summary>
    /// Get all portfolios for a user
    /// </summary>
    [Get("/api/portfolios/user/{userId}")]
    Task<List<PortfolioResponseDto>> GetUserPortfoliosAsync(string userId);

    /// <summary>
    /// Get portfolio by ID
    /// </summary>
    [Get("/api/portfolios/{id}")]
    Task<PortfolioResponseDto> GetPortfolioByIdAsync(string id);

    /// <summary>
    /// Create new portfolio
    /// </summary>
    [Post("/api/portfolios")]
    Task<PortfolioResponseDto> CreatePortfolioAsync([Body] CreatePortfolioRequest request);

    /// <summary>
    /// Update portfolio
    /// </summary>
    [Put("/api/portfolios/{id}")]
    Task<PortfolioResponseDto> UpdatePortfolioAsync(string id, [Body] UpdatePortfolioRequest request);

    /// <summary>
    /// Delete portfolio
    /// </summary>
    [Delete("/api/portfolios/{id}")]
    Task DeletePortfolioAsync(string id);
}

