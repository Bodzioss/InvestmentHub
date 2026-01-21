using InvestmentHub.Domain.Commands;
using InvestmentHub.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.Handlers.Commands;

public class CancelTransactionHandler : IRequestHandler<CancelTransactionCommand, CancelTransactionResult>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<CancelTransactionHandler> _logger;

    public CancelTransactionHandler(ApplicationDbContext dbContext, ILogger<CancelTransactionHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<CancelTransactionResult> Handle(CancelTransactionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var transaction = await _dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken);
            if (transaction == null) return CancelTransactionResult.Failure("Transaction not found");

            transaction.Cancel();
            await _dbContext.SaveChangesAsync(cancellationToken);

            return CancelTransactionResult.Success();
        }
        catch (InvalidOperationException ex)
        {
            return CancelTransactionResult.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel transaction");
            return CancelTransactionResult.Failure($"Failed: {ex.Message}");
        }
    }
}
