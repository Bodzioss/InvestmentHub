using MediatR;
using Microsoft.AspNetCore.Mvc;
using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Repositories;
using InvestmentHub.Domain.Entities;
using AutoMapper;
using InvestmentHub.Contracts;

namespace InvestmentHub.API.Controllers;

/// <summary>
/// Controller for managing portfolios using MediatR and CQRS pattern.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PortfoliosController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PortfoliosController> _logger;
    private readonly IMapper _mapper;
    private readonly IMarketPriceRepository _marketPriceRepository;

    /// <summary>
    /// Initializes a new instance of the PortfoliosController class.
    /// </summary>
    /// <param name="mediator">The MediatR mediator</param>
    /// <param name="logger">The logger</param>
    /// <param name="mapper">The AutoMapper instance</param>
    /// <param name="marketPriceRepository">The market price repository</param>
    public PortfoliosController(
        IMediator mediator,
        ILogger<PortfoliosController> logger,
        IMapper mapper,
        IMarketPriceRepository marketPriceRepository)
    {
        _mediator = mediator;
        _logger = logger;
        _mapper = mapper;
        _marketPriceRepository = marketPriceRepository;
    }

    /// <summary>
    /// Creates a new portfolio.
    /// </summary>
    /// <param name="request">The create portfolio request</param>
    /// <returns>The result of the operation</returns>
    [HttpPost]
    public async Task<IActionResult> CreatePortfolio([FromBody] CreatePortfolioRequest request)
    {
        try
        {
            var command = _mapper.Map<CreatePortfolioCommand>(request);
            var result = await _mediator.Send(command);

            if (result.IsSuccess && result.PortfolioId != null)
            {
                // Return full response to update UI immediately
                var response = new PortfolioResponseDto
                {
                    Id = result.PortfolioId.Value.ToString(),
                    Name = request.Name,
                    Description = request.Description,
                    OwnerId = request.OwnerId,
                    CreatedDate = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    TotalValue = new MoneyResponseDto { Amount = 0, Currency = request.Currency },
                    TotalCost = new MoneyResponseDto { Amount = 0, Currency = request.Currency },
                    UnrealizedGainLoss = new MoneyResponseDto { Amount = 0, Currency = request.Currency },
                    ActiveInvestmentCount = 0,
                    Currency = request.Currency
                };

                return CreatedAtAction(
                    nameof(GetPortfolio),
                    new { portfolioId = result.PortfolioId.Value.ToString() },
                    response);
            }

            return BadRequest(new { Error = result.ErrorMessage });
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid data in create portfolio request");
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating portfolio");
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets all portfolios for a user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>The portfolios data</returns>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserPortfolios([FromRoute] string userId)
    {
        try
        {
            var query = new GetUserPortfoliosQuery(UserId.FromString(userId));
            var result = await _mediator.Send(query);

            if (result.IsSuccess && result.Portfolios != null)
            {
                var response = result.Portfolios.Select(p => new PortfolioResponseDto
                {
                    Id = p.PortfolioId.Value.ToString(),
                    Name = p.Name,
                    Description = p.Description,
                    OwnerId = userId, // Use the userId from route parameter
                    CreatedDate = p.CreatedAt, // Fix: Map from CreatedAt to CreatedDate
                    LastUpdated = p.LastUpdated,
                    TotalValue = new MoneyResponseDto
                    {
                        Amount = p.TotalValue.Amount,
                        Currency = p.TotalValue.Currency.ToString()
                    },
                    TotalCost = new MoneyResponseDto
                    {
                        Amount = p.TotalCost.Amount,
                        Currency = p.TotalCost.Currency.ToString()
                    },
                    UnrealizedGainLoss = new MoneyResponseDto
                    {
                        Amount = p.UnrealizedGainLoss.Amount,
                        Currency = p.UnrealizedGainLoss.Currency.ToString()
                    },
                    ActiveInvestmentCount = p.ActiveInvestmentCount,
                    Currency = p.TotalValue.Currency.ToString()
                }).ToList();
                return Ok(response);
            }

            return BadRequest(new { Error = result.ErrorMessage });
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid user ID format: {UserId}", userId);
            return BadRequest(new { Error = "Invalid user ID format" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving portfolios for user {UserId}", userId);
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a portfolio by ID.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID</param>
    /// <returns>The portfolio data</returns>
    [HttpGet("{portfolioId}")]
    public async Task<IActionResult> GetPortfolio([FromRoute] string portfolioId)
    {
        try
        {
            var query = new GetPortfolioQuery(PortfolioId.FromString(portfolioId));
            var result = await _mediator.Send(query);

            if (result.IsSuccess && result.Portfolio != null)
            {
                var p = result.Portfolio;

                // Get investments to calculate TotalCost and UnrealizedGainLoss
                var investmentsQuery = new GetInvestmentsQuery(PortfolioId.FromString(portfolioId));
                var investmentsResult = await _mediator.Send(investmentsQuery);

                decimal totalValue = 0;
                decimal totalCost = 0;
                decimal unrealizedGainLoss = 0;

                if (investmentsResult.IsSuccess && investmentsResult.Investments != null)
                {
                    foreach (var inv in investmentsResult.Investments)
                    {
                        // TotalValue = sum of current values
                        totalValue += inv.CurrentValue.Amount;

                        // InvestmentSummary already has TotalCost calculated
                        totalCost += inv.TotalCost.Amount;

                        // InvestmentSummary already has UnrealizedGainLoss calculated
                        unrealizedGainLoss += inv.UnrealizedGainLoss.Amount;
                    }
                }

                var response = new PortfolioResponseDto
                {
                    Id = p.Id.ToString(),
                    Name = p.Name,
                    Description = p.Description,
                    OwnerId = p.OwnerId.ToString(),
                    CreatedDate = p.CreatedAt,
                    LastUpdated = p.LastUpdated,
                    TotalValue = new MoneyResponseDto
                    {
                        Amount = totalValue,
                        Currency = p.Currency
                    },
                    TotalCost = new MoneyResponseDto
                    {
                        Amount = totalCost,
                        Currency = p.Currency
                    },
                    UnrealizedGainLoss = new MoneyResponseDto
                    {
                        Amount = unrealizedGainLoss,
                        Currency = p.Currency
                    },
                    ActiveInvestmentCount = p.InvestmentCount,
                    Currency = p.Currency
                };
                return Ok(response);
            }

            return BadRequest(new { Error = result.ErrorMessage });
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid portfolio ID format: {PortfolioId}", portfolioId);
            return BadRequest(new { Error = "Invalid portfolio ID format" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }

    /// <summary>
    /// Updates portfolio details.
    /// </summary>
    /// <param name="id">The portfolio ID</param>
    /// <param name="request">The update request</param>
    /// <returns>No content</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDetails([FromRoute] string id, [FromBody] UpdatePortfolioRequest request)
    {
        if (id != request.PortfolioId)
        {
            return BadRequest(new { Error = "Portfolio ID mismatch" });
        }

        try
        {
            var command = new UpdatePortfolioDetailsCommand(
                PortfolioId.FromString(id),
                request.Name ?? string.Empty,
                request.Description
            );

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return NoContent();
            }

            return BadRequest(new { Error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating portfolio {PortfolioId}", id);
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }

    /// <summary>
    /// Deletes a portfolio.
    /// </summary>
    /// <param name="id">The portfolio ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] string id)
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var userId = UserId.FromString(userIdClaim.Value);

            var command = new DeletePortfolioCommand(
                PortfolioId.FromString(id),
                userId
            );

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return NoContent();
            }

            return BadRequest(new { Error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting portfolio {PortfolioId}", id);
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets portfolio performance history.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID</param>
    /// <returns>Historical performance data</returns>
    [HttpGet("{portfolioId}/performance")]
    public async Task<IActionResult> GetPerformanceHistory([FromRoute] string portfolioId)
    {
        try
        {
            _logger.LogInformation("Getting performance history for portfolio {PortfolioId}", portfolioId);

            // Get all investments for this portfolio
            var investmentsQuery = new GetInvestmentsQuery(PortfolioId.FromString(portfolioId));
            var investmentsResult = await _mediator.Send(investmentsQuery);

            if (!investmentsResult.IsSuccess || investmentsResult.Investments == null || !investmentsResult.Investments.Any())
            {
                _logger.LogWarning("No investments found for portfolio {PortfolioId}", portfolioId);

                // Even without investments, return structure starting from portfolio creation
                var portfolioQuery = new GetPortfolioQuery(PortfolioId.FromString(portfolioId));
                var portfolioResult = await _mediator.Send(portfolioQuery);

                if (portfolioResult.IsSuccess && portfolioResult.Portfolio != null)
                {
                    var startFromCreation = portfolioResult.Portfolio.CreatedAt.Date;
                    return Ok(new PortfolioPerformanceResponse
                    {
                        DataPoints = new List<PerformanceDataPoint>
                        {
                            new PerformanceDataPoint { Date = startFromCreation, Value = 0 }
                        },
                        InvestmentValues = new Dictionary<string, List<PerformanceDataPoint>>(),
                        StartDate = startFromCreation,
                        EndDate = DateTime.UtcNow.Date,
                        Currency = portfolioResult.Portfolio.Currency
                    });
                }

                return Ok(new PortfolioPerformanceResponse
                {
                    DataPoints = new List<PerformanceDataPoint>(),
                    InvestmentValues = new Dictionary<string, List<PerformanceDataPoint>>(),
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow,
                    Currency = "USD"
                });
            }

            var investments = investmentsResult.Investments.ToList();

            // Use portfolio creation date, not earliest investment purchase date
            var portfolioQueryForDate = new GetPortfolioQuery(PortfolioId.FromString(portfolioId));
            var portfolioResultForDate = await _mediator.Send(portfolioQueryForDate);

            DateTime startDate;
            if (portfolioResultForDate.IsSuccess && portfolioResultForDate.Portfolio != null)
            {
                startDate = portfolioResultForDate.Portfolio.CreatedAt.Date;
            }
            else
            {
                // Fallback to earliest investment if portfolio query fails
                startDate = investments.Min(i => i.PurchaseDate).Date;
            }

            var endDate = DateTime.UtcNow.Date;

            // Get unique symbols
            var symbols = investments.Select(i => i.Symbol.Ticker).Distinct().ToList();

            // Fetch price history for all symbols
            var priceHistories = new Dictionary<string, List<CachedMarketPrice>>();
            foreach (var symbol in symbols)
            {
                var prices = await _marketPriceRepository.GetPriceHistoryAsync(
                    symbol,
                    startDate,
                    endDate,
                    CancellationToken.None);
                priceHistories[symbol] = prices;
            }

            // Calculate value for each investment on each date
            var investmentValues = new Dictionary<string, List<PerformanceDataPoint>>();
            var allDates = new SortedSet<DateTime>();

            foreach (var investment in investments)
            {
                var symbol = investment.Symbol.Ticker;
                var investmentId = investment.Id.Value.ToString();
                var quantity = investment.Quantity;
                var purchaseDate = investment.PurchaseDate.Date;

                var dataPoints = new List<PerformanceDataPoint>();

                if (!priceHistories.TryGetValue(symbol, out var prices) || !prices.Any())
                {
                    continue; // Skip if no price history
                }

                // Add day before purchase with value 0
                dataPoints.Add(new PerformanceDataPoint
                {
                    Date = purchaseDate.AddDays(-1),
                    Value = 0
                });
                allDates.Add(purchaseDate.AddDays(-1));

                // Calculate value for each date from purchase onwards
                decimal? lastKnownPrice = null;
                for (var date = purchaseDate; date <= endDate; date = date.AddDays(1))
                {
                    // Find price for this date (or use last known)
                    var priceForDate = prices.FirstOrDefault(p => p.FetchedAt.Date == date);
                    if (priceForDate != null)
                    {
                        lastKnownPrice = priceForDate.Price;
                    }

                    if (lastKnownPrice.HasValue)
                    {
                        var value = quantity * lastKnownPrice.Value;
                        dataPoints.Add(new PerformanceDataPoint
                        {
                            Date = date,
                            Value = value
                        });
                        allDates.Add(date);
                    }
                }

                investmentValues[investmentId] = dataPoints;
            }

            // Aggregate to get total portfolio value for each date
            var aggregatedDataPoints = new List<PerformanceDataPoint>();
            foreach (var date in allDates)
            {
                decimal totalValue = 0;
                foreach (var invValues in investmentValues.Values)
                {
                    // Find the value for this investment on this date (or closest earlier)
                    var dataPoint = invValues
                        .Where(dp => dp.Date <= date)
                        .OrderByDescending(dp => dp.Date)
                        .FirstOrDefault();

                    if (dataPoint != null)
                    {
                        totalValue += dataPoint.Value;
                    }
                }

                aggregatedDataPoints.Add(new PerformanceDataPoint
                {
                    Date = date,
                    Value = totalValue
                });
            }

            var response = new PortfolioPerformanceResponse
            {
                DataPoints = aggregatedDataPoints.OrderBy(d => d.Date).ToList(),
                InvestmentValues = investmentValues,
                StartDate = startDate,
                EndDate = endDate,
                Currency = investments.FirstOrDefault()?.CurrentValue.Currency.ToString() ?? "USD"
            };

            _logger.LogInformation("Returning {Count} data points for portfolio {PortfolioId}",
                aggregatedDataPoints.Count, portfolioId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance history for portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, new { Error = "Failed to get performance history" });
        }
    }

    /// <summary>
    /// Gets cached prices for a symbol (for debugging).
    /// </summary>
    /// <param name="symbol">The symbol ticker</param>
    /// <returns>List of cached prices</returns>
    [HttpGet("cached-prices/{symbol}")]
    public async Task<IActionResult> GetCachedPrices([FromRoute] string symbol)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-30); // Last 30 days
            var endDate = DateTime.UtcNow;

            var prices = await _marketPriceRepository.GetPriceHistoryAsync(
                symbol,
                startDate,
                endDate,
                CancellationToken.None);

            var response = prices.Select(p => new
            {
                symbol = p.Symbol,
                price = p.Price,
                currency = p.Currency,
                fetchedAt = p.FetchedAt,
                source = p.Source
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached prices for symbol {Symbol}", symbol);
            return StatusCode(500, new { Error = "Failed to get cached prices" });
        }
    }
}
