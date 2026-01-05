using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.ReadModels;
using InvestmentHub.Infrastructure.Data;
using Marten;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.Handlers.Commands;

public class RecordSellTransactionHandler : IRequestHandler<RecordSellTransactionCommand, RecordSellTransactionResult>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDocumentSession _session;
    private readonly ILogger<RecordSellTransactionHandler> _logger;

    public RecordSellTransactionHandler(
        ApplicationDbContext dbContext,
        IDocumentSession session,
        ILogger<RecordSellTransactionHandler> logger)
    {
        _dbContext = dbContext;
        _session = session;
        _logger = logger;
    }

    public async Task<RecordSellTransactionResult> Handle(RecordSellTransactionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate portfolio exists using Marten read model
            var portfolio = await _session.LoadAsync<PortfolioReadModel>(request.PortfolioId.Value, cancellationToken);
            if (portfolio == null) return RecordSellTransactionResult.Failure($"Portfolio {request.PortfolioId.Value} not found");

            var totalBought = await _dbContext.Transactions
                .Where(t => t.PortfolioId == request.PortfolioId &&
                            t.Symbol.Ticker == request.Symbol.Ticker &&
                            t.Symbol.Exchange == request.Symbol.Exchange &&
                            t.Type == Domain.Enums.TransactionType.BUY &&
                            t.Status == Domain.Enums.TransactionStatus.Active)
                .SumAsync(t => t.Quantity ?? 0, cancellationToken);

            var totalSold = await _dbContext.Transactions
                .Where(t => t.PortfolioId == request.PortfolioId &&
                            t.Symbol.Ticker == request.Symbol.Ticker &&
                            t.Symbol.Exchange == request.Symbol.Exchange &&
                            t.Type == Domain.Enums.TransactionType.SELL &&
                            t.Status == Domain.Enums.TransactionStatus.Active)
                .SumAsync(t => t.Quantity ?? 0, cancellationToken);

            if (totalBought - totalSold < request.Quantity)
                return RecordSellTransactionResult.Failure($"Insufficient holdings. Current: {totalBought - totalSold}, trying to sell: {request.Quantity}");

            var transaction = Transaction.RecordSell(request.PortfolioId, request.Symbol, request.Quantity, request.SalePrice, request.TransactionDate, request.Fee, request.Notes);
            await _dbContext.Transactions.AddAsync(transaction, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return RecordSellTransactionResult.Success(transaction.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record SELL transaction");
            _dbContext.ChangeTracker.Clear();
            return RecordSellTransactionResult.Failure($"Failed: {ex.Message}");
        }
    }
}
