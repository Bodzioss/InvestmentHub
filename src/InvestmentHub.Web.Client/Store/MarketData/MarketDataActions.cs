using InvestmentHub.Contracts;

namespace InvestmentHub.Web.Client.Store.MarketData;

// Fetch Price
public record FetchPriceAction(string Symbol);
public record FetchPriceSuccessAction(MarketPriceDto Price);
public record FetchPriceFailureAction(string ErrorMessage);

// Fetch History
public record FetchHistoryAction(string Symbol);
public record FetchHistorySuccessAction(IEnumerable<MarketPriceDto> History);
public record FetchHistoryFailureAction(string ErrorMessage);

// Import History
public record ImportHistoryAction(string Symbol);
public record ImportHistorySuccessAction(string Symbol);
