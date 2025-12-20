using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.Commands;
using InvestmentHub.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.Handlers.Commands;

public class RecordSellTransactionHandler : IRequestHandler<RecordSellTransactionCommand, RecordSellTransactionResult>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RecordSellTransactionHandler> _logger;

    public RecordSellTransactionHandler(ApplicationDbContext dbContext, ILogger<RecordSellTransactionHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<RecordSellTransactionResult> Handle(RecordSellTransactionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var portfolioExists = await _dbContext.Portfolios.AnyAsync(p => p.Id == request.PortfolioId, cancellationToken);
            if (!portfolioExists) return RecordSellTransactionResult.Failure("Portfolio not found");

            var totalBought = await _dbContext.Transactions
                .Where(t => t.PortfolioId == request.PortfolioId && t.Symbol == request.Symbol && t.Type == Domain.Enums.TransactionType.BUY && t.Status == Domain.Enums.TransactionStatus.Active)
                .SumAsync(t => t.Quantity ?? 0, cancellationToken);

            var totalSold = await _dbContext.Transactions
                .Where(t => t.PortfolioId == request.PortfolioId && t.Symbol == request.Symbol && t.Type == Domain.Enums.TransactionType.SELL && t.Status == Domain.Enums.TransactionStatus.Active)
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
            return RecordSellTransactionResult.Failure($"Failed: {ex.Message}");
        }
    }
}
