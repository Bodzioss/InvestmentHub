using MediatR;
using Microsoft.AspNetCore.Mvc;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Contracts.Income;

namespace InvestmentHub.API.Controllers;

/// <summary>
/// Controller for viewing income reports (dividends and interest).
/// </summary>
[ApiController]
[Route("api/portfolios/{portfolioId:guid}/income")]
public class IncomeController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<IncomeController> _logger;

    public IncomeController(IMediator mediator, ILogger<IncomeController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets income summary for a portfolio (dividends + interest).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IncomeSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetIncomeSummary(
        Guid portfolioId,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        // Get all transactions for the portfolio
        var query = new GetTransactionsQuery(new PortfolioId(portfolioId));
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        // Filter to income transactions (DIVIDEND and INTEREST)
        var incomeTransactions = result.Transactions!
            .Where(t => t.Type == TransactionType.DIVIDEND || t.Type == TransactionType.INTEREST)
            .Where(t => t.Status == TransactionStatus.Active);

        // Apply date filters if provided
        if (year.HasValue)
            incomeTransactions = incomeTransactions.Where(t => t.TransactionDate.Year == year.Value);
        if (month.HasValue)
            incomeTransactions = incomeTransactions.Where(t => t.TransactionDate.Month == month.Value);

        var transactionsList = incomeTransactions.ToList();

        // Calculate totals
        var totalDividends = transactionsList
            .Where(t => t.Type == TransactionType.DIVIDEND)
            .Sum(t => t.NetAmount?.Amount ?? 0);

        var totalInterest = transactionsList
            .Where(t => t.Type == TransactionType.INTEREST)
            .Sum(t => t.NetAmount?.Amount ?? 0);

        // Group by symbol
        var bySymbol = transactionsList
            .GroupBy(t => new { t.Symbol?.Ticker, t.Symbol?.Exchange })
            .Select(g => new IncomeBySymbol
            {
                Ticker = g.Key.Ticker ?? string.Empty,
                Exchange = g.Key.Exchange ?? string.Empty,
                Dividends = g.Where(t => t.Type == TransactionType.DIVIDEND).Sum(t => t.NetAmount?.Amount ?? 0),
                Interest = g.Where(t => t.Type == TransactionType.INTEREST).Sum(t => t.NetAmount?.Amount ?? 0),
                Total = g.Sum(t => t.NetAmount?.Amount ?? 0)
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        // Group by month
        var byMonth = transactionsList
            .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
            .Select(g => new IncomeByMonth
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Dividends = g.Where(t => t.Type == TransactionType.DIVIDEND).Sum(t => t.NetAmount?.Amount ?? 0),
                Interest = g.Where(t => t.Type == TransactionType.INTEREST).Sum(t => t.NetAmount?.Amount ?? 0),
                Total = g.Sum(t => t.NetAmount?.Amount ?? 0)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToList();

        var currency = transactionsList.FirstOrDefault()?.NetAmount?.Currency.ToString() ?? "USD";

        return Ok(new IncomeSummaryResponse
        {
            TotalDividends = totalDividends,
            TotalInterest = totalInterest,
            TotalIncome = totalDividends + totalInterest,
            Currency = currency,
            BySymbol = bySymbol,
            ByMonth = byMonth
        });
    }

    /// <summary>
    /// Gets dividends for a portfolio.
    /// </summary>
    [HttpGet("dividends")]
    [ProducesResponseType(typeof(IncomeSummaryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDividends(Guid portfolioId, [FromQuery] int? year = null)
    {
        var query = new GetTransactionsQuery(new PortfolioId(portfolioId));
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        var dividends = result.Transactions!
            .Where(t => t.Type == TransactionType.DIVIDEND && t.Status == TransactionStatus.Active);

        if (year.HasValue)
            dividends = dividends.Where(t => t.TransactionDate.Year == year.Value);

        var dividendsList = dividends.ToList();

        var bySymbol = dividendsList
            .GroupBy(t => new { t.Symbol?.Ticker, t.Symbol?.Exchange })
            .Select(g => new IncomeBySymbol
            {
                Ticker = g.Key.Ticker ?? string.Empty,
                Exchange = g.Key.Exchange ?? string.Empty,
                Dividends = g.Sum(t => t.NetAmount?.Amount ?? 0),
                Interest = 0,
                Total = g.Sum(t => t.NetAmount?.Amount ?? 0)
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        return Ok(new IncomeSummaryResponse
        {
            TotalDividends = dividendsList.Sum(t => t.NetAmount?.Amount ?? 0),
            TotalInterest = 0,
            TotalIncome = dividendsList.Sum(t => t.NetAmount?.Amount ?? 0),
            Currency = dividendsList.FirstOrDefault()?.NetAmount?.Currency.ToString() ?? "USD",
            BySymbol = bySymbol,
            ByMonth = []
        });
    }

    /// <summary>
    /// Gets interest payments for a portfolio.
    /// </summary>
    [HttpGet("interest")]
    [ProducesResponseType(typeof(IncomeSummaryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInterest(Guid portfolioId, [FromQuery] int? year = null)
    {
        var query = new GetTransactionsQuery(new PortfolioId(portfolioId));
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        var interest = result.Transactions!
            .Where(t => t.Type == TransactionType.INTEREST && t.Status == TransactionStatus.Active);

        if (year.HasValue)
            interest = interest.Where(t => t.TransactionDate.Year == year.Value);

        var interestList = interest.ToList();

        var bySymbol = interestList
            .GroupBy(t => new { t.Symbol?.Ticker, t.Symbol?.Exchange })
            .Select(g => new IncomeBySymbol
            {
                Ticker = g.Key.Ticker ?? string.Empty,
                Exchange = g.Key.Exchange ?? string.Empty,
                Dividends = 0,
                Interest = g.Sum(t => t.NetAmount?.Amount ?? 0),
                Total = g.Sum(t => t.NetAmount?.Amount ?? 0)
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        return Ok(new IncomeSummaryResponse
        {
            TotalDividends = 0,
            TotalInterest = interestList.Sum(t => t.NetAmount?.Amount ?? 0),
            TotalIncome = interestList.Sum(t => t.NetAmount?.Amount ?? 0),
            Currency = interestList.FirstOrDefault()?.NetAmount?.Currency.ToString() ?? "USD",
            BySymbol = bySymbol,
            ByMonth = []
        });
    }
}
