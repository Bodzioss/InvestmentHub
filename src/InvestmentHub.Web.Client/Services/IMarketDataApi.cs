using InvestmentHub.Contracts;
using Refit;

namespace InvestmentHub.Web.Client.Services;

public interface IMarketDataApi
{
    [Get("/api/market/price/{symbol}")]
    Task<MarketPriceDto> GetPriceAsync(string symbol);

    [Get("/api/market/history/{symbol}")]
    Task<IEnumerable<MarketPriceDto>> GetHistoryAsync(string symbol, [AliasAs("from")] DateTime? from = null, [AliasAs("to")] DateTime? to = null);

    [Post("/api/market/import/{symbol}")]
    Task ImportHistoryAsync(string symbol);
}
