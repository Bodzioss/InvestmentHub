using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.ReadModels;
using InvestmentHub.Infrastructure.Data;
using Marten;
using MediatR;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.Handlers.Commands;

/// <summary>
/// Handler for RecordBuyTransactionCommand.
/// Records a BUY transaction for a portfolio.
/// </summary>
public class RecordBuyTransactionHandler : IRequestHandler<RecordBuyTransactionCommand, RecordBuyTransactionResult>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDocumentSession _session;
    private readonly ILogger<RecordBuyTransactionHandler> _logger;

    public RecordBuyTransactionHandler(
        ApplicationDbContext dbContext,
        IDocumentSession session,
        ILogger<RecordBuyTransactionHandler> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RecordBuyTransactionResult> Handle(
        RecordBuyTransactionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Recording BUY transaction for portfolio {PortfolioId}, symbol {Symbol}",
                request.PortfolioId.Value, request.Symbol.Ticker);

            // Validate portfolio exists using Marten read model
            var portfolio = await _session.LoadAsync<PortfolioReadModel>(request.PortfolioId.Value, cancellationToken);

            if (portfolio == null)
            {
                _logger.LogWarning("Portfolio {PortfolioId} not found in Marten", request.PortfolioId.Value);
                return RecordBuyTransactionResult.Failure($"Portfolio {request.PortfolioId.Value} not found");
            }

            // Create Transaction aggregate (generates BuyTransactionRecorded event)
            var transaction = Transaction.RecordBuy(
                request.PortfolioId,
                request.Symbol,
                request.Quantity,
                request.PricePerUnit,
                request.TransactionDate,
                request.Fee,
                request.MaturityDate,
                request.Notes);

            // Add to database
            await _dbContext.Transactions.AddAsync(transaction, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully recorded BUY transaction {TransactionId}",
                transaction.Id.Value);

            return RecordBuyTransactionResult.Success(transaction.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record BUY transaction for portfolio {PortfolioId}",
                request.PortfolioId.Value);
            return RecordBuyTransactionResult.Failure($"Failed to record transaction: {ex.Message}");
        }
    }
}
