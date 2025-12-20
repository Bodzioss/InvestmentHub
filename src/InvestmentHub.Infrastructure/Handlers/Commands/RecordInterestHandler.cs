using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.Commands;
using InvestmentHub.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.Handlers.Commands;

public class RecordInterestHandler : IRequestHandler<RecordInterestCommand, RecordInterestResult>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RecordInterestHandler> _logger;

    public RecordInterestHandler(ApplicationDbContext dbContext, ILogger<RecordInterestHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<RecordInterestResult> Handle(RecordInterestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var portfolioExists = await _dbContext.Portfolios.AnyAsync(p => p.Id == request.PortfolioId, cancellationToken);
            if (!portfolioExists) return RecordInterestResult.Failure("Portfolio not found");

            var transaction = Transaction.RecordInterest(request.PortfolioId, request.Symbol, request.GrossAmount, request.PaymentDate, request.TaxRate, request.Notes);
            await _dbContext.Transactions.AddAsync(transaction, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return RecordInterestResult.Success(transaction.Id, transaction.NetAmount!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record INTEREST");
            return RecordInterestResult.Failure($"Failed: {ex.Message}");
        }
    }
}
