using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Interfaces;

public interface IMarketDataProvider
{
    Task<MarketPrice?> GetLatestPriceAsync(Symbol symbol, CancellationToken cancellationToken = default, List<string>? traceLogs = null);
    Task<IEnumerable<MarketPrice>> GetHistoricalPricesAsync(Symbol symbol, DateTime from, DateTime to, CancellationToken cancellationToken = default, List<string>? traceLogs = null);
    Task<IEnumerable<SecurityInfo>> SearchSecuritiesAsync(string query, CancellationToken cancellationToken = default);
}

public record SecurityInfo(string Symbol, string Name, string Type, string Exchange, string Currency);
