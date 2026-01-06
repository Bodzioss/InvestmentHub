using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Interfaces;

/// <summary>
/// Service for fetching currency exchange rates.
/// </summary>
public interface IExchangeRateService
{
    /// <summary>
    /// Gets the exchange rate to convert from source currency to target currency.
    /// </summary>
    /// <param name="fromCurrency">Source currency</param>
    /// <param name="toCurrency">Target currency</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exchange rate (multiplier) or null if not found</returns>
    Task<decimal?> GetExchangeRateAsync(Currency fromCurrency, Currency toCurrency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts an amount from source currency to target currency.
    /// </summary>
    /// <param name="amount">Amount in source currency</param>
    /// <param name="fromCurrency">Source currency</param>
    /// <param name="toCurrency">Target currency</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Converted money in target currency or null if rate not found</returns>
    Task<Money?> ConvertAsync(decimal amount, Currency fromCurrency, Currency toCurrency, CancellationToken cancellationToken = default);
}
