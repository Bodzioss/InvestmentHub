using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.ReadModels;
using InvestmentHub.Infrastructure.Data;
using Marten;
using MediatR;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.Handlers.Commands;

public class RecordInterestHandler : IRequestHandler<RecordInterestCommand, RecordInterestResult>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDocumentSession _session;
    private readonly ILogger<RecordInterestHandler> _logger;

    public RecordInterestHandler(
        ApplicationDbContext dbContext,
        IDocumentSession session,
        ILogger<RecordInterestHandler> logger)
    {
        _dbContext = dbContext;
        _session = session;
        _logger = logger;
    }

    public async Task<RecordInterestResult> Handle(RecordInterestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate portfolio exists using Marten read model
            var portfolio = await _session.LoadAsync<PortfolioReadModel>(request.PortfolioId.Value, cancellationToken);
            if (portfolio == null) return RecordInterestResult.Failure($"Portfolio {request.PortfolioId.Value} not found");

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
