using InvestmentHub.Contracts;
using Refit;

namespace InvestmentHub.Web.Client.Services;

/// <summary>
/// Refit API client for Investments endpoints
/// </summary>
public interface IInvestmentsApi
{
    /// <summary>
    /// Get all investments for a portfolio
    /// </summary>
    [Get("/api/investments/portfolio/{portfolioId}")]
    Task<List<InvestmentResponseDto>> GetPortfolioInvestmentsAsync(string portfolioId);

    /// <summary>
    /// Get investment by ID
    /// </summary>
    [Get("/api/investments/{id}")]
    Task<InvestmentResponseDto> GetInvestmentByIdAsync(string id);

    /// <summary>
    /// Add new investment
    /// </summary>
    [Post("/api/investments")]
    Task<InvestmentResponseDto> AddInvestmentAsync([Body] AddInvestmentRequest request);

    /// <summary>
    /// Update investment value
    /// </summary>
    [Put("/api/investments/{id}/value")]
    Task<InvestmentResponseDto> UpdateInvestmentValueAsync(string id, [Body] UpdateInvestmentValueRequest request);

    /// <summary>
    /// Sell investment
    /// </summary>
    [Post("/api/investments/{id}/sell")]
    Task<InvestmentResponseDto> SellInvestmentAsync(string id, [Body] SellInvestmentRequest request);

    /// <summary>
    /// Delete investment
    /// </summary>
    [Delete("/api/investments/{id}")]
    Task DeleteInvestmentAsync(string id);
}

