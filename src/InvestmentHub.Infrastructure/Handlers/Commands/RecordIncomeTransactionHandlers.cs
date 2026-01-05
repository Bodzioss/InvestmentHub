using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.ReadModels;
using InvestmentHub.Infrastructure.Data;
using Marten;
using MediatR;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.Handlers.Commands;

/// <summary>
/// Handler for RecordDividendTransactionCommand.
/// </summary>
public class RecordDividendTransactionHandler : IRequestHandler<RecordDividendTransactionCommand, RecordIncomeTransactionResult>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDocumentSession _session;
    private readonly ILogger<RecordDividendTransactionHandler> _logger;

    public RecordDividendTransactionHandler(
        ApplicationDbContext dbContext,
        IDocumentSession session,
        ILogger<RecordDividendTransactionHandler> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RecordIncomeTransactionResult> Handle(
        RecordDividendTransactionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Recording DIVIDEND for portfolio {PortfolioId}, symbol {Symbol}",
                request.PortfolioId.Value, request.Symbol.Ticker);

            var portfolio = await _session.LoadAsync<PortfolioReadModel>(request.PortfolioId.Value, cancellationToken);
            if (portfolio == null)
            {
                return RecordIncomeTransactionResult.Failure($"Portfolio {request.PortfolioId.Value} not found");
            }

            var transaction = Transaction.RecordDividend(
                request.PortfolioId,
                request.Symbol,
                request.GrossAmount,
                request.PaymentDate,
                request.TaxRate,
                request.Notes);

            await _dbContext.Transactions.AddAsync(transaction, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Recorded DIVIDEND {TransactionId}", transaction.Id.Value);
            return RecordIncomeTransactionResult.Success(transaction.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record DIVIDEND for portfolio {PortfolioId}",
                request.PortfolioId.Value);
            _dbContext.ChangeTracker.Clear();
            return RecordIncomeTransactionResult.Failure($"Failed: {ex.Message}");
        }
    }
}

/// <summary>
/// Handler for RecordInterestTransactionCommand.
/// </summary>
public class RecordInterestTransactionHandler : IRequestHandler<RecordInterestTransactionCommand, RecordIncomeTransactionResult>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDocumentSession _session;
    private readonly ILogger<RecordInterestTransactionHandler> _logger;

    public RecordInterestTransactionHandler(
        ApplicationDbContext dbContext,
        IDocumentSession session,
        ILogger<RecordInterestTransactionHandler> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RecordIncomeTransactionResult> Handle(
        RecordInterestTransactionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Recording INTEREST for portfolio {PortfolioId}, symbol {Symbol}",
                request.PortfolioId.Value, request.Symbol.Ticker);

            var portfolio = await _session.LoadAsync<PortfolioReadModel>(request.PortfolioId.Value, cancellationToken);
            if (portfolio == null)
            {
                return RecordIncomeTransactionResult.Failure($"Portfolio {request.PortfolioId.Value} not found");
            }

            var transaction = Transaction.RecordInterest(
                request.PortfolioId,
                request.Symbol,
                request.GrossAmount,
                request.PaymentDate,
                request.TaxRate,
                request.Notes);

            await _dbContext.Transactions.AddAsync(transaction, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Recorded INTEREST {TransactionId}", transaction.Id.Value);
            return RecordIncomeTransactionResult.Success(transaction.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record INTEREST for portfolio {PortfolioId}",
                request.PortfolioId.Value);
            _dbContext.ChangeTracker.Clear();
            return RecordIncomeTransactionResult.Failure($"Failed: {ex.Message}");
        }
    }
}
