using InvestmentHub.Domain.Commands;
using InvestmentHub.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.Handlers.Commands;

public class UpdateTransactionHandler : IRequestHandler<UpdateTransactionCommand, UpdateTransactionResult>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UpdateTransactionHandler> _logger;

    public UpdateTransactionHandler(ApplicationDbContext dbContext, ILogger<UpdateTransactionHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<UpdateTransactionResult> Handle(UpdateTransactionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var transaction = await _dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken);
            if (transaction == null) return UpdateTransactionResult.Failure("Transaction not found");

            transaction.Update(request.Quantity, request.PricePerUnit, request.Fee, request.GrossAmount, request.TaxRate, request.TransactionDate, request.Notes);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return UpdateTransactionResult.Success();
        }
        catch (InvalidOperationException ex)
        {
            return UpdateTransactionResult.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update transaction");
            return UpdateTransactionResult.Failure($"Failed: {ex.Message}");
        }
    }
}
