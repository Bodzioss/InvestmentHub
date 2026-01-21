using InvestmentHub.Domain.Queries;
using InvestmentHub.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.Handlers.Queries;

public class GetTransactionQueryHandler : IRequestHandler<GetTransactionQuery, GetTransactionResult>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetTransactionQueryHandler> _logger;

    public GetTransactionQueryHandler(ApplicationDbContext dbContext, ILogger<GetTransactionQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<GetTransactionResult> Handle(GetTransactionQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var transaction = await _dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken);
            if (transaction == null) return GetTransactionResult.Failure("Transaction not found");

            return GetTransactionResult.Success(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transaction");
            return GetTransactionResult.Failure($"Failed: {ex.Message}");
        }
    }
}
