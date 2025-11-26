using Fluxor;
using InvestmentHub.Contracts;

namespace InvestmentHub.Web.Client.Store.Investment;

/// <summary>
/// State for managing investments in the selected portfolio
/// </summary>
[FeatureState]
public record InvestmentState
{
    /// <summary>
    /// List of investments for the current portfolio
    /// </summary>
    public List<InvestmentResponseDto> Investments { get; init; } = new();

    /// <summary>
    /// Currently selected portfolio ID
    /// </summary>
    public string? SelectedPortfolioId { get; init; }

    /// <summary>
    /// Indicates if investments are being loaded
    /// </summary>
    public bool IsLoading { get; init; }

    /// <summary>
    /// Error message if loading failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    private InvestmentState() { } // Required for creating initial state

    public InvestmentState(List<InvestmentResponseDto> investments, string? selectedPortfolioId, bool isLoading, string? errorMessage)
    {
        Investments = investments;
        SelectedPortfolioId = selectedPortfolioId;
        IsLoading = isLoading;
        ErrorMessage = errorMessage;
    }
}

