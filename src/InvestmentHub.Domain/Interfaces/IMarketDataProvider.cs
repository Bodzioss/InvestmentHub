using InvestmentHub.Domain.Entities;

namespace InvestmentHub.Domain.Interfaces;

public interface IMarketDataProvider
{
    Task<MarketPrice?> GetLatestPriceAsync(string symbol, CancellationToken cancellationToken = default);
    Task<IEnumerable<MarketPrice>> GetHistoricalPricesAsync(string symbol, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<IEnumerable<SecurityInfo>> SearchSecuritiesAsync(string query, CancellationToken cancellationToken = default);
}

public record SecurityInfo(string Symbol, string Name, string Type, string Exchange, string Currency);
