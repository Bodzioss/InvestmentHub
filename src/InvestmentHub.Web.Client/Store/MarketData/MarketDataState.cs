using Fluxor;
using InvestmentHub.Contracts;

namespace InvestmentHub.Web.Client.Store.MarketData;

[FeatureState]
public class MarketDataState
{
    public Dictionary<string, MarketPriceDto> Prices { get; }
    public List<MarketPriceDto> SelectedHistory { get; }
    public bool IsLoading { get; }
    public string? ErrorMessage { get; }

    public MarketDataState()
    {
        Prices = new Dictionary<string, MarketPriceDto>();
        SelectedHistory = new List<MarketPriceDto>();
        IsLoading = false;
        ErrorMessage = null;
    }

    public MarketDataState(
        Dictionary<string, MarketPriceDto> prices,
        List<MarketPriceDto> selectedHistory,
        bool isLoading,
        string? errorMessage)
    {
        Prices = prices;
        SelectedHistory = selectedHistory;
        IsLoading = isLoading;
        ErrorMessage = errorMessage;
    }
}
