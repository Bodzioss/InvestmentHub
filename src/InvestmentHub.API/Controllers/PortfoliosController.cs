using MediatR;
using Microsoft.AspNetCore.Mvc;
using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.ValueObjects;
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

    /// <summary>
    /// Initializes a new instance of the PortfoliosController class.
    /// </summary>
    /// <param name="mediator">The MediatR mediator</param>
    /// <param name="logger">The logger</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public PortfoliosController(IMediator mediator, ILogger<PortfoliosController> logger, IMapper mapper)
    {
        _mediator = mediator;
        _logger = logger;
        _mapper = mapper;
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
                        Amount = p.TotalValue,
                        Currency = p.Currency
                    },
                    TotalCost = new MoneyResponseDto
                    {
                         Amount = 0, // TODO
                         Currency = p.Currency
                    },
                    UnrealizedGainLoss = new MoneyResponseDto
                    {
                         Amount = 0, // TODO
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
}
