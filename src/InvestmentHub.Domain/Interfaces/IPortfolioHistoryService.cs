using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Interfaces;

/// <summary>
/// Service for reconstructing portfolio history and performance over time.
/// </summary>
public interface IPortfolioHistoryService
{
    /// <summary>
    /// Calculates the portfolio's total value for each day within the specified date range.
    /// </summary>
    /// <param name="portfolioId">The ID of the portfolio</param>
    /// <param name="from">Start date</param>
    /// <param name="to">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of daily values in the portfolio's base currency</returns>
    Task<IEnumerable<DatePoint>> GetPortfolioHistoryAsync(Guid portfolioId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
}

public record DatePoint(DateTime Date, decimal Value);
