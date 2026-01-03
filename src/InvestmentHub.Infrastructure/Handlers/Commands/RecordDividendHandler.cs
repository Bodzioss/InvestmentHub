using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.ReadModels;
using InvestmentHub.Infrastructure.Data;
using Marten;
using MediatR;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.Handlers.Commands;

public class RecordDividendHandler : IRequestHandler<RecordDividendCommand, RecordDividendResult>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDocumentSession _session;
    private readonly ILogger<RecordDividendHandler> _logger;

    public RecordDividendHandler(
        ApplicationDbContext dbContext,
        IDocumentSession session,
        ILogger<RecordDividendHandler> logger)
    {
        _dbContext = dbContext;
        _session = session;
        _logger = logger;
    }

    public async Task<RecordDividendResult> Handle(RecordDividendCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate portfolio exists using Marten read model
            var portfolio = await _session.LoadAsync<PortfolioReadModel>(request.PortfolioId.Value, cancellationToken);
            if (portfolio == null) return RecordDividendResult.Failure($"Portfolio {request.PortfolioId.Value} not found");

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
