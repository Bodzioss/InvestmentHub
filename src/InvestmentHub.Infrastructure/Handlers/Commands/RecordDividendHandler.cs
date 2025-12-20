using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.Commands;
using InvestmentHub.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.Handlers.Commands;

public class RecordDividendHandler : IRequestHandler<RecordDividendCommand, RecordDividendResult>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RecordDividendHandler> _logger;

    public RecordDividendHandler(ApplicationDbContext dbContext, ILogger<RecordDividendHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<RecordDividendResult> Handle(RecordDividendCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var portfolioExists = await _dbContext.Portfolios.AnyAsync(p => p.Id == request.PortfolioId, cancellationToken);
            if (!portfolioExists) return RecordDividendResult.Failure("Portfolio not found");

            var transaction = Transaction.RecordDividend(request.PortfolioId, request.Symbol, request.GrossAmount, request.PaymentDate, request.TaxRate, request.Notes);
            await _dbContext.Transactions.AddAsync(transaction, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return RecordDividendResult.Success(transaction.Id, transaction.NetAmount!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record DIVIDEND");
            return RecordDividendResult.Failure($"Failed: {ex.Message}");
        }
    }
}
