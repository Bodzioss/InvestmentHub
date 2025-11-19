using Fluxor;
using InvestmentHub.Contracts;

namespace InvestmentHub.Web.Client.Store.Portfolio;

[FeatureState]
public record PortfolioState
{
    public bool IsLoading { get; init; }
    public List<PortfolioResponseDto> Portfolios { get; init; } = new();
    public string? ErrorMessage { get; init; }

    private PortfolioState() { }

    public PortfolioState(bool isLoading, List<PortfolioResponseDto> portfolios, string? errorMessage)
    {
        IsLoading = isLoading;
        Portfolios = portfolios;
        ErrorMessage = errorMessage;
    }
}

