using MediatR;
using Microsoft.AspNetCore.Mvc;
using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Repositories;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Interfaces; // Added for IPortfolioHistoryService
using AutoMapper;
using InvestmentHub.Contracts;
using InvestmentHub.Domain.Enums;
using Marten; // Ensure Marten is available

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
    private readonly IPortfolioHistoryService _portfolioHistoryService; // Injected service

    /// <summary>
    /// Initializes a new instance of the PortfoliosController class.
    /// </summary>
    public PortfoliosController(
        IMediator mediator,
        ILogger<PortfoliosController> logger,
        IMapper mapper,
        IMarketPriceRepository marketPriceRepository,
        IPortfolioHistoryService portfolioHistoryService)
    {
        _mediator = mediator;
        _logger = logger;
        _mapper = mapper;
        _marketPriceRepository = marketPriceRepository;
        _portfolioHistoryService = portfolioHistoryService;
    }

    /// <summary>
    /// Creates a new portfolio.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreatePortfolio([FromBody] CreatePortfolioRequest request)
    {
        try
        {
            var command = _mapper.Map<CreatePortfolioCommand>(request);
            var result = await _mediator.Send(command);

            if (result.IsSuccess && result.PortfolioId != null)
            {
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
    /// Gets portfolio performance history using Time Machine service.
    /// </summary>
    [HttpGet("{portfolioId}/performance")]
    public async Task<IActionResult> GetPerformanceHistory([FromRoute] string portfolioId)
    {
        try
        {
            _logger.LogInformation("Getting performance history for portfolio {PortfolioId}", portfolioId);

            var pid = PortfolioId.FromString(portfolioId);
            var from = DateTime.UtcNow.AddYears(-5); // Defines max history
            var to = DateTime.UtcNow;

            var history = await _portfolioHistoryService.GetPortfolioHistoryAsync(pid.Value, from, to);

            // Fetch portfolio metadata for currency
            var portfolioResult = await _mediator.Send(new GetPortfolioQuery(pid));
            var currency = portfolioResult.Portfolio?.Currency ?? Currency.PLN.ToString();

            var result = new PortfolioPerformanceResponse
            {
                DataPoints = history.Select(h => new PerformanceDataPoint
                {
                    Date = h.Date,
                    Value = h.Value,
                    TotalCost = 0 // Cost tracking not yet implemented in history service
                }).ToList(),
                InvestmentValues = new Dictionary<string, List<PerformanceDataPoint>>(),
                StartDate = history.FirstOrDefault()?.Date ?? from,
                EndDate = to,
                Currency = currency
            };

            return Ok(result);
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
