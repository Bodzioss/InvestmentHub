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
                return CreatedAtAction(
                    nameof(GetPortfolio),
                    new { portfolioId = result.PortfolioId.Value.ToString() },
                    new { PortfolioId = result.PortfolioId.Value.ToString() });
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
                    CreatedDate = p.CreatedAt,
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
                    ActiveInvestmentCount = p.ActiveInvestmentCount
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
                var response = _mapper.Map<PortfolioResponseDto>(result.Portfolio);
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
}
