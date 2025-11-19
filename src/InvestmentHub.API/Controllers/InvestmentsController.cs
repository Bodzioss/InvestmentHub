using MediatR;
using Microsoft.AspNetCore.Mvc;
using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Enums;
using AutoMapper;
using InvestmentHub.API.DTOs;

namespace InvestmentHub.API.Controllers;

/// <summary>
/// Controller for managing investments using MediatR and CQRS pattern.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InvestmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<InvestmentsController> _logger;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the InvestmentsController class.
    /// </summary>
    /// <param name="mediator">The MediatR mediator</param>
    /// <param name="logger">The logger</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public InvestmentsController(IMediator mediator, ILogger<InvestmentsController> logger, IMapper mapper)
    {
        _mediator = mediator;
        _logger = logger;
        _mapper = mapper;
    }

    /// <summary>
    /// Adds a new investment to a portfolio.
    /// </summary>
    /// <param name="request">The add investment request</param>
    /// <returns>The result of the operation</returns>
    [HttpPost]
    public async Task<IActionResult> AddInvestment([FromBody] AddInvestmentRequest request)
    {
        try
        {
            var command = _mapper.Map<AddInvestmentCommand>(request);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(new { InvestmentId = result.InvestmentId.Value });
            }

            return BadRequest(new { Error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding investment");
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }

    /// <summary>
    /// Updates an investment's current value.
    /// </summary>
    /// <param name="request">The update value request</param>
    /// <returns>The result of the operation</returns>
    [HttpPut("value")]
    public async Task<IActionResult> UpdateInvestmentValue([FromBody] UpdateInvestmentValueRequest request)
    {
        try
        {
            var command = _mapper.Map<UpdateInvestmentValueCommand>(request);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(new { UpdatedValue = result.UpdatedValue!.Amount });
            }

            return BadRequest(new { Error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating investment value");
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }

    /// <summary>
    /// Sells an investment (fully or partially).
    /// </summary>
    /// <param name="request">The sell investment request</param>
    /// <returns>The result of the operation</returns>
    [HttpPost("sell")]
    public async Task<IActionResult> SellInvestment([FromBody] SellInvestmentRequest request)
    {
        try
        {
            var command = _mapper.Map<SellInvestmentCommand>(request);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(new
                {
                    RealizedProfitLoss = result.RealizedProfitLoss!.Amount,
                    Currency = result.RealizedProfitLoss!.Currency.ToString(),
                    QuantitySold = result.QuantitySold,
                    IsCompleteSale = result.IsCompleteSale
                });
            }

            return BadRequest(new { Error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selling investment");
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets all investments for a portfolio.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID</param>
    /// <returns>The investments data</returns>
    [HttpGet("portfolio/{portfolioId}")]
    public async Task<IActionResult> GetInvestmentsByPortfolio([FromRoute] string portfolioId)
    {
        try
        {
            var query = new GetInvestmentsQuery(PortfolioId.FromString(portfolioId));
            var result = await _mediator.Send(query);

            if (result.IsSuccess && result.Investments != null)
            {
                var response = result.Investments.Select(inv =>
                {
                    var dto = _mapper.Map<InvestmentResponseDto>(inv);
                    dto.PortfolioId = portfolioId; // Set portfolioId from route parameter
                    return dto;
                }).ToList();
                return Ok(response);
            }

            return BadRequest(new { Error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving investments for portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets an investment by ID.
    /// </summary>
    /// <param name="investmentId">The investment ID</param>
    /// <returns>The investment data</returns>
    [HttpGet("{investmentId}")]
    public async Task<IActionResult> GetInvestment([FromRoute] string investmentId)
    {
        try
        {
            var query = new GetInvestmentQuery(InvestmentId.FromString(investmentId));
            var result = await _mediator.Send(query);

            if (result.IsSuccess && result.Investment != null)
            {
                var response = _mapper.Map<InvestmentResponseDto>(result.Investment);
                return Ok(response);
            }

            return NotFound(new { Error = "Investment not found" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid investment ID format: {InvestmentId}", investmentId);
            return BadRequest(new { Error = "Invalid investment ID format" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving investment");
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }

    // ==================== Event Sourcing Incompatible Endpoints ====================
    // The following endpoints are DISABLED because they are incompatible with Event Sourcing principles:
    // 
    // 1. DELETE /api/Investments/{id} - In Event Sourcing, we don't delete data; events are immutable.
    //    Alternative: Use POST /api/Investments/sell to mark investment as sold.
    //    Future: Can add ArchiveInvestment command if needed (soft delete via event).
    //
    // 2. PUT /api/Investments/{id} - In Event Sourcing, we don't modify historical data.
    //    Alternative: Use PUT /api/Investments/value to update current market value only.
    //    Future: Can add CorrectInvestment command for error corrections (with audit trail).
    //
    // If you need these features, please discuss with the team about proper Event Sourcing patterns.
    // ================================================================================

    /* DISABLED - Event Sourcing Incompatible
    [HttpDelete("{investmentId}")]
    public async Task<IActionResult> DeleteInvestment([FromRoute] string investmentId)
    {
        return StatusCode(501, new { Error = "Delete is not supported in Event Sourcing. Use 'Sell Investment' instead." });
    }
    */

    /* DISABLED - Event Sourcing Incompatible
    [HttpPut("{investmentId}")]
    public async Task<IActionResult> UpdateInvestment([FromRoute] string investmentId, [FromBody] UpdateInvestmentRequest request)
    {
        return StatusCode(501, new { Error = "Direct updates are not supported in Event Sourcing. Use 'Update Investment Value' instead." });
    }
    */
}
