using MediatR;
using Microsoft.AspNetCore.Mvc;
using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Contracts.Transactions;

namespace InvestmentHub.API.Controllers;

/// <summary>
/// Controller for managing portfolio transactions.
/// </summary>
[ApiController]
[Route("api/portfolios/{portfolioId:guid}/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(IMediator mediator, ILogger<TransactionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Records a BUY transaction.
    /// </summary>
    [HttpPost("buy")]
    [ProducesResponseType(typeof(TransactionCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordBuy(Guid portfolioId, [FromBody] RecordBuyRequest request)
    {
        var symbol = new Symbol(request.Ticker, request.Exchange, Enum.Parse<AssetType>(request.AssetType, true));
        var currency = Enum.Parse<Currency>(request.Currency, true);
        var pricePerUnit = new Money(request.PricePerUnit, currency);
        var fee = request.Fee.HasValue ? new Money(request.Fee.Value, currency) : null;

        var command = new RecordBuyTransactionCommand(
            new PortfolioId(portfolioId),
            symbol,
            request.Quantity,
            pricePerUnit,
            request.TransactionDate,
            fee,
            request.MaturityDate,
            request.Notes);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return CreatedAtAction(
            nameof(GetTransaction),
            new { portfolioId, transactionId = result.TransactionId!.Value },
            new TransactionCreatedResponse
            {
                TransactionId = result.TransactionId.Value,
                Message = "BUY transaction recorded successfully"
            });
    }

    /// <summary>
    /// Records a SELL transaction.
    /// </summary>
    [HttpPost("sell")]
    [ProducesResponseType(typeof(TransactionCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordSell(Guid portfolioId, [FromBody] RecordSellRequest request)
    {
        var symbol = new Symbol(request.Ticker, request.Exchange, Enum.Parse<AssetType>(request.AssetType, true));
        var currency = Enum.Parse<Currency>(request.Currency, true);
        var salePrice = new Money(request.SalePrice, currency);
        var fee = request.Fee.HasValue ? new Money(request.Fee.Value, currency) : null;

        var command = new RecordSellTransactionCommand(
            new PortfolioId(portfolioId),
            symbol,
            request.Quantity,
            salePrice,
            request.TransactionDate,
            fee,
            request.Notes);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return CreatedAtAction(
            nameof(GetTransaction),
            new { portfolioId, transactionId = result.TransactionId!.Value },
            new TransactionCreatedResponse
            {
                TransactionId = result.TransactionId.Value,
                Message = "SELL transaction recorded successfully"
            });
    }

    /// <summary>
    /// Records a DIVIDEND payment.
    /// </summary>
    [HttpPost("dividend")]
    [ProducesResponseType(typeof(TransactionCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordDividend(Guid portfolioId, [FromBody] RecordDividendRequest request)
    {
        var symbol = new Symbol(request.Ticker, request.Exchange, AssetType.Stock);
        var currency = Enum.Parse<Currency>(request.Currency, true);
        var grossAmount = new Money(request.GrossAmount, currency);

        var command = new RecordDividendCommand(
            new PortfolioId(portfolioId),
            symbol,
            grossAmount,
            request.PaymentDate,
            request.TaxRate,
            request.Notes);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return CreatedAtAction(
            nameof(GetTransaction),
            new { portfolioId, transactionId = result.TransactionId!.Value },
            new TransactionCreatedResponse
            {
                TransactionId = result.TransactionId.Value,
                Message = "DIVIDEND recorded successfully",
                NetAmount = result.NetAmount?.Amount
            });
    }

    /// <summary>
    /// Records an INTEREST payment.
    /// </summary>
    [HttpPost("interest")]
    [ProducesResponseType(typeof(TransactionCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordInterest(Guid portfolioId, [FromBody] RecordInterestRequest request)
    {
        var symbol = new Symbol(request.Ticker, request.Exchange, AssetType.Bond);
        var currency = Enum.Parse<Currency>(request.Currency, true);
        var grossAmount = new Money(request.GrossAmount, currency);

        var command = new RecordInterestCommand(
            new PortfolioId(portfolioId),
            symbol,
            grossAmount,
            request.PaymentDate,
            request.TaxRate,
            request.Notes);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return CreatedAtAction(
            nameof(GetTransaction),
            new { portfolioId, transactionId = result.TransactionId!.Value },
            new TransactionCreatedResponse
            {
                TransactionId = result.TransactionId.Value,
                Message = "INTEREST recorded successfully",
                NetAmount = result.NetAmount?.Amount
            });
    }

    /// <summary>
    /// Gets all transactions for a portfolio.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(TransactionsListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(Guid portfolioId)
    {
        var query = new GetTransactionsQuery(new PortfolioId(portfolioId));
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        var responses = result.Transactions!.Select(MapToResponse).ToList();

        return Ok(new TransactionsListResponse
        {
            Transactions = responses,
            TotalCount = responses.Count
        });
    }

    /// <summary>
    /// Gets a specific transaction.
    /// </summary>
    [HttpGet("{transactionId:guid}")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransaction(Guid portfolioId, Guid transactionId)
    {
        var query = new GetTransactionQuery(new TransactionId(transactionId));
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return NotFound(new { error = result.ErrorMessage });

        return Ok(MapToResponse(result.Transaction!));
    }

    /// <summary>
    /// Updates a transaction.
    /// </summary>
    [HttpPut("{transactionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTransaction(
        Guid portfolioId,
        Guid transactionId,
        [FromBody] UpdateTransactionRequest request)
    {
        // First get the transaction to get its currency
        var getQuery = new GetTransactionQuery(new TransactionId(transactionId));
        var getResult = await _mediator.Send(getQuery);
        if (!getResult.IsSuccess)
            return NotFound(new { error = getResult.ErrorMessage });

        var transaction = getResult.Transaction!;
        var currency = transaction.PricePerUnit?.Currency ?? transaction.GrossAmount?.Currency ?? Currency.USD;

        var command = new UpdateTransactionCommand(
            new TransactionId(transactionId),
            request.Quantity,
            request.PricePerUnit.HasValue ? new Money(request.PricePerUnit.Value, currency) : null,
            request.Fee.HasValue ? new Money(request.Fee.Value, currency) : null,
            request.GrossAmount.HasValue ? new Money(request.GrossAmount.Value, currency) : null,
            request.TaxRate,
            request.TransactionDate,
            request.Notes);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return NoContent();
    }

    /// <summary>
    /// Cancels a transaction.
    /// </summary>
    [HttpDelete("{transactionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelTransaction(Guid portfolioId, Guid transactionId)
    {
        var command = new CancelTransactionCommand(new TransactionId(transactionId));
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return NoContent();
    }

    private static TransactionResponse MapToResponse(Domain.Aggregates.Transaction transaction)
    {
        return new TransactionResponse
        {
            TransactionId = transaction.Id.Value,
            Type = transaction.Type.ToString(),
            Status = transaction.Status.ToString(),
            Ticker = transaction.Symbol?.Ticker ?? string.Empty,
            Exchange = transaction.Symbol?.Exchange ?? string.Empty,
            AssetType = transaction.Symbol?.AssetType.ToString() ?? string.Empty,
            Quantity = transaction.Quantity,
            PricePerUnit = transaction.PricePerUnit?.Amount,
            Currency = transaction.PricePerUnit?.Currency.ToString() ?? transaction.GrossAmount?.Currency.ToString(),
            Fee = transaction.Fee?.Amount,
            GrossAmount = transaction.GrossAmount?.Amount,
            NetAmount = transaction.NetAmount?.Amount,
            TaxRate = transaction.TaxRate,
            TransactionDate = transaction.TransactionDate,
            MaturityDate = transaction.MaturityDate,
            Notes = transaction.Notes
        };
    }
}
