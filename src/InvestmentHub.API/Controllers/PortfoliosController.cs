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
                var portfolioResponses = new List<PortfolioResponseDto>();

                foreach (var p in result.Portfolios)
                {
                    // Get positions from transactions to calculate real values
                    var positionsQuery = new GetPositionsQuery(p.PortfolioId);
                    var positionsResult = await _mediator.Send(positionsQuery);

                    decimal totalValue = 0;
                    decimal totalCost = 0;
                    decimal unrealizedGainLoss = 0;
                    int positionCount = 0;
                    string currency = p.TotalValue.Currency.ToString();

                    if (positionsResult.IsSuccess && positionsResult.Positions != null && positionsResult.Positions.Any())
                    {
                        foreach (var pos in positionsResult.Positions)
                        {
                            totalValue += pos.CurrentValue.Amount;
                            totalCost += pos.TotalCost.Amount;
                            unrealizedGainLoss += pos.UnrealizedGainLoss.Amount;
                        }
                        positionCount = positionsResult.Positions.Count();
                        // Use currency from first position if available
                        currency = positionsResult.Positions.First().CurrentValue.Currency.ToString();
                    }

                    portfolioResponses.Add(new PortfolioResponseDto
                    {
                        Id = p.PortfolioId.Value.ToString(),
                        Name = p.Name,
                        Description = p.Description,
                        OwnerId = userId,
                        CreatedDate = p.CreatedAt,
                        LastUpdated = p.LastUpdated,
                        TotalValue = new MoneyResponseDto
                        {
                            Amount = totalValue,
                            Currency = currency
                        },
                        TotalCost = new MoneyResponseDto
                        {
                            Amount = totalCost,
                            Currency = currency
                        },
                        UnrealizedGainLoss = new MoneyResponseDto
                        {
                            Amount = unrealizedGainLoss,
                            Currency = currency
                        },
                        ActiveInvestmentCount = positionCount,
                        Currency = currency
                    });
                }

                return Ok(portfolioResponses);
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

                // Get positions from transactions to calculate TotalCost and UnrealizedGainLoss
                var positionsQuery = new GetPositionsQuery(PortfolioId.FromString(portfolioId));
                var positionsResult = await _mediator.Send(positionsQuery);

                decimal totalValue = 0;
                decimal totalCost = 0;
                decimal unrealizedGainLoss = 0;
                int positionCount = 0;

                if (positionsResult.IsSuccess && positionsResult.Positions != null)
                {
                    foreach (var pos in positionsResult.Positions)
                    {
                        totalValue += pos.CurrentValue.Amount;
                        totalCost += pos.TotalCost.Amount;
                        unrealizedGainLoss += pos.UnrealizedGainLoss.Amount;
                    }
                    positionCount = positionsResult.Positions.Count();
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
                    ActiveInvestmentCount = positionCount,
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

            // Get portfolio to find creation date
            var portfolioQuery = new GetPortfolioQuery(PortfolioId.FromString(portfolioId));
            var portfolioResult = await _mediator.Send(portfolioQuery);

            if (!portfolioResult.IsSuccess || portfolioResult.Portfolio == null)
            {
                return BadRequest(new { Error = "Portfolio not found" });
            }

            var portfolio = portfolioResult.Portfolio;
            var portfolioCreationDate = portfolio.CreatedAt.Date;

            // Get positions from transactions to get current holdings
            var positionsQuery = new GetPositionsQuery(PortfolioId.FromString(portfolioId));
            var positionsResult = await _mediator.Send(positionsQuery);

            if (!positionsResult.IsSuccess || positionsResult.Positions == null || !positionsResult.Positions.Any())
            {
                _logger.LogWarning("No positions found for portfolio {PortfolioId}", portfolioId);

                return Ok(new PortfolioPerformanceResponse
                {
                    DataPoints = new List<PerformanceDataPoint>
                    {
                        new PerformanceDataPoint { Date = portfolioCreationDate, Value = 0, TotalCost = 0 }
                    },
                    InvestmentValues = new Dictionary<string, List<PerformanceDataPoint>>(),
                    StartDate = portfolioCreationDate,
                    EndDate = DateTime.UtcNow.Date,
                    Currency = portfolio.Currency
                });
            }

            var positions = positionsResult.Positions.ToList();

            // Get unique symbols and their quantities/costs from positions
            var symbols = positions.Select(p => p.Symbol.Ticker).Distinct().ToList();

            // Chart starts from day before first transaction or portfolio creation
            var startDate = portfolioCreationDate.AddDays(-1);
            var endDate = DateTime.UtcNow.Date;

            // Fetch price history for all symbols
            var priceHistories = new Dictionary<string, Dictionary<DateTime, decimal>>();
            foreach (var symbol in symbols)
            {
                var prices = await _marketPriceRepository.GetPriceHistoryAsync(
                    symbol,
                    startDate,
                    endDate,
                    CancellationToken.None);

                priceHistories[symbol] = prices.ToDictionary(
                    p => p.FetchedAt.Date,
                    p => p.Price
                );
            }

            // Build data points based on positions
            var allDates = new SortedSet<DateTime>();
            var symbolValues = new Dictionary<string, List<PerformanceDataPoint>>();

            // For TotalCost, we use the current total cost from positions (simplified - assumes all purchased at once)
            var totalCostFromPositions = positions.Sum(p => p.TotalCost.Amount);

            foreach (var position in positions)
            {
                var symbol = position.Symbol.Ticker;
                var quantity = position.TotalQuantity;
                var dataPoints = new List<PerformanceDataPoint>();

                if (!priceHistories.TryGetValue(symbol, out var pricesByDate) || !pricesByDate.Any())
                {
                    // Use current price if no history
                    dataPoints.Add(new PerformanceDataPoint
                    {
                        Date = endDate,
                        Value = position.CurrentValue.Amount
                    });
                    allDates.Add(endDate);
                }
                else
                {
                    decimal? lastKnownPrice = null;
                    for (var date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        if (date < portfolioCreationDate)
                        {
                            dataPoints.Add(new PerformanceDataPoint { Date = date, Value = 0 });
                            allDates.Add(date);
                        }
                        else
                        {
                            if (pricesByDate.TryGetValue(date, out var price))
                            {
                                lastKnownPrice = price;
                            }

                            if (lastKnownPrice.HasValue)
                            {
                                var value = quantity * lastKnownPrice.Value;
                                dataPoints.Add(new PerformanceDataPoint { Date = date, Value = value });
                                allDates.Add(date);
                            }
                        }
                    }
                }

                symbolValues[symbol] = dataPoints;
            }

            // Aggregate to get total portfolio value for each date
            var aggregatedDataPoints = new List<PerformanceDataPoint>();
            foreach (var date in allDates)
            {
                decimal totalValue = 0;
                foreach (var symValues in symbolValues.Values)
                {
                    var dataPoint = symValues
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
                    Value = totalValue,
                    TotalCost = date >= portfolioCreationDate ? totalCostFromPositions : 0
                });
            }

            var response = new PortfolioPerformanceResponse
            {
                DataPoints = aggregatedDataPoints.OrderBy(d => d.Date).ToList(),
                InvestmentValues = symbolValues,
                StartDate = startDate,
                EndDate = endDate,
                Currency = portfolio.Currency
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
