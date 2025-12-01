using InvestmentHub.Contracts;
using Refit;

namespace InvestmentHub.Web.Client.Services;

public interface IInstrumentsApi
{
    [Get("/api/instruments")]
    Task<List<InstrumentDto>> GetInstrumentsAsync(
        [AliasAs("query")] string? query = null,
        [AliasAs("assetType")] string? assetType = null,
        [AliasAs("exchange")] string? exchange = null);
}
