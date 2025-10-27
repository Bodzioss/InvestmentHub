using MediatR;
using Microsoft.AspNetCore.Mvc;
using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Enums;

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

    /// <summary>
    /// Initializes a new instance of the InvestmentsController class.
    /// </summary>
    /// <param name="mediator">The MediatR mediator</param>
    /// <param name="logger">The logger</param>
    public InvestmentsController(IMediator mediator, ILogger<InvestmentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
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
            var command = new AddInvestmentCommand(
                PortfolioId.FromString(request.PortfolioId),
                new Symbol(request.Symbol.Ticker, request.Symbol.Exchange, Enum.Parse<AssetType>(request.Symbol.AssetType)),
                new Money(request.PurchasePrice.Amount, Enum.Parse<Currency>(request.PurchasePrice.Currency)),
                request.Quantity,
                request.PurchaseDate);

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
    /// <param name="investmentId">The investment ID</param>
    /// <param name="request">The update value request</param>
    /// <returns>The result of the operation</returns>
    [HttpPut("{investmentId}/value")]
    public async Task<IActionResult> UpdateInvestmentValue(
        [FromRoute] string investmentId,
        [FromBody] UpdateInvestmentValueRequest request)
    {
        try
        {
            var command = new UpdateInvestmentValueCommand(
                InvestmentId.FromString(investmentId),
                new Money(request.CurrentPrice.Amount, Enum.Parse<Currency>(request.CurrentPrice.Currency)));

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
                return Ok(new
                {
                    InvestmentId = result.Investment.Id.Value,
                    Symbol = result.Investment.Symbol.Ticker,
                    Quantity = result.Investment.Quantity,
                    CurrentValue = result.Investment.CurrentValue.Amount,
                    Currency = result.Investment.CurrentValue.Currency.ToString()
                });
            }

            return NotFound(new { Error = "Investment not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving investment");
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }
}

/// <summary>
/// Request model for adding an investment.
/// </summary>
public record AddInvestmentRequest
{
    /// <summary>Gets or sets the portfolio ID</summary>
    public string PortfolioId { get; set; } = string.Empty;

    /// <summary>Gets or sets the symbol information</summary>
    public SymbolRequest Symbol { get; set; } = new();

    /// <summary>Gets or sets the purchase price</summary>
    public MoneyRequest PurchasePrice { get; set; } = new();

    /// <summary>Gets or sets the quantity</summary>
    public decimal Quantity { get; set; }

    /// <summary>Gets or sets the purchase date</summary>
    public DateTime PurchaseDate { get; set; }
}

/// <summary>
/// Request model for updating investment value.
/// </summary>
public record UpdateInvestmentValueRequest
{
    /// <summary>Gets or sets the current price</summary>
    public MoneyRequest CurrentPrice { get; set; } = new();
}

/// <summary>
/// Request model for symbol information.
/// </summary>
public record SymbolRequest
{
    /// <summary>Gets or sets the ticker symbol</summary>
    public string Ticker { get; set; } = string.Empty;

    /// <summary>Gets or sets the exchange</summary>
    public string Exchange { get; set; } = string.Empty;

    /// <summary>Gets or sets the asset type</summary>
    public string AssetType { get; set; } = string.Empty;
}

/// <summary>
/// Request model for money information.
/// </summary>
public record MoneyRequest
{
    /// <summary>Gets or sets the amount</summary>
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the currency</summary>
    public string Currency { get; set; } = string.Empty;
}
