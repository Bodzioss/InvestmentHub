using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.Handlers.Queries;

public class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, GetTransactionsResult>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetTransactionsQueryHandler> _logger;

    public GetTransactionsQueryHandler(ApplicationDbContext dbContext, ILogger<GetTransactionsQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<GetTransactionsResult> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _dbContext.Transactions.Where(t => t.PortfolioId == request.PortfolioId && t.Status == TransactionStatus.Active);

            if (request.TypeFilter.HasValue) query = query.Where(t => t.Type == request.TypeFilter.Value);
            if (request.SymbolFilter != null) query = query.Where(t => t.Symbol == request.SymbolFilter);
            if (request.StartDate.HasValue) query = query.Where(t => t.TransactionDate >= request.StartDate.Value);
            if (request.EndDate.HasValue) query = query.Where(t => t.TransactionDate <= request.EndDate.Value);

            var totalCount = await query.CountAsync(cancellationToken);
            var transactions = await query.OrderByDescending(t => t.TransactionDate).Skip(request.Offset).Take(request.Limit).ToListAsync(cancellationToken);

            return GetTransactionsResult.Success(transactions, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transactions");
            return GetTransactionsResult.Failure($"Failed: {ex.Message}");
        }
    }
}
