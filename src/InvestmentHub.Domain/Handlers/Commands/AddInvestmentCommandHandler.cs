using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.ReadModels;
using MediatR;
using Marten;
using Microsoft.Extensions.Logging;
using InvestmentHub.Domain.Services;
using InvestmentHub.Domain.Common;

namespace InvestmentHub.Domain.Handlers.Commands;

/// <summary>
/// Handler for AddInvestmentCommand.
/// Responsible for adding a new investment to a portfolio using Event Sourcing with Marten.
/// </summary>
public class AddInvestmentCommandHandler : IRequestHandler<AddInvestmentCommand, AddInvestmentResult>
{
    private readonly IDocumentSession _session;
    private readonly ILogger<AddInvestmentCommandHandler> _logger;
    private readonly ICorrelationIdEnricher _correlationIdEnricher;
    private readonly IMetricsRecorder _metricsRecorder;

    /// <summary>
    /// Initializes a new instance of the AddInvestmentCommandHandler class.
    /// </summary>
    /// <param name="session">The Marten document session for event sourcing</param>
    /// <param name="logger">The logger</param>
    /// <param name="correlationIdEnricher">The Correlation ID enricher for Marten sessions</param>
    /// <param name="metricsRecorder">The metrics recorder service</param>
    public AddInvestmentCommandHandler(
        IDocumentSession session,
        ILogger<AddInvestmentCommandHandler> logger,
        ICorrelationIdEnricher correlationIdEnricher,
        IMetricsRecorder metricsRecorder)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _correlationIdEnricher = correlationIdEnricher ?? throw new ArgumentNullException(nameof(correlationIdEnricher));
        _metricsRecorder = metricsRecorder ?? throw new ArgumentNullException(nameof(metricsRecorder));
    }

    /// <summary>
    /// Handles the AddInvestmentCommand using Event Sourcing.
    /// </summary>
    /// <param name="request">The command request</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The result of the operation</returns>
    public async Task<AddInvestmentResult> Handle(AddInvestmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Adding investment to portfolio {PortfolioId}", 
                request.PortfolioId.Value);

            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();
            
            // Validate the request
            if (request.Quantity <= 0)
            {
                _logger.LogWarning("Invalid quantity: {Quantity}", request.Quantity);
                return AddInvestmentResult.Failure("Quantity must be positive");
            }

            if (request.PurchaseDate > DateTime.UtcNow)
            {
                _logger.LogWarning("Invalid purchase date: {PurchaseDate}", request.PurchaseDate);
                return AddInvestmentResult.Failure("Purchase date cannot be in the future");
            }

            // 1. Load portfolio read model to validate it exists and is not closed
            var portfolio = await _session.Query<PortfolioReadModel>()
                .FirstOrDefaultAsync(p => p.Id == request.PortfolioId.Value, cancellationToken);
            
            if (portfolio == null)
            {
                _logger.LogWarning("Portfolio {PortfolioId} not found", request.PortfolioId.Value);
                return AddInvestmentResult.Failure("Portfolio not found");
            }

            if (portfolio.IsClosed)
            {
                _logger.LogWarning("Portfolio {PortfolioId} is closed", request.PortfolioId.Value);
                return AddInvestmentResult.Failure("Cannot add investment to a closed portfolio");
            }

            // 2. Check for duplicate symbol in portfolio (query InvestmentReadModel)
            var existingInvestment = await _session.Query<InvestmentReadModel>()
                .Where(i => i.PortfolioId == request.PortfolioId.Value 
                    && i.Ticker == request.Symbol.Ticker 
                    && i.Status == InvestmentStatus.Active)
                .AnyAsync(cancellationToken);

            if (existingInvestment)
            {
                _logger.LogWarning("Investment with symbol {Symbol} already exists in portfolio {PortfolioId}", 
                    request.Symbol.Ticker, request.PortfolioId.Value);
                return AddInvestmentResult.Failure($"Investment with symbol {request.Symbol.Ticker} already exists in this portfolio");
            }

            // 3. Check portfolio investment limits
            var investmentCount = await _session.Query<InvestmentReadModel>()
                .Where(i => i.PortfolioId == request.PortfolioId.Value)
                .CountAsync(cancellationToken);

            const int maxInvestmentsPerPortfolio = 100;
            if (investmentCount >= maxInvestmentsPerPortfolio)
            {
                _logger.LogWarning("Portfolio {PortfolioId} has reached maximum investment limit: {Count}", 
                    request.PortfolioId.Value, investmentCount);
                return AddInvestmentResult.Failure($"Portfolio cannot have more than {maxInvestmentsPerPortfolio} investments");
            }

            // 4. Generate new investment ID
            var investmentId = InvestmentId.New();

            // 5. Create investment aggregate (generates InvestmentAddedEvent)
            var investmentAggregate = InvestmentAggregate.Create(
                investmentId,
                request.PortfolioId,
                request.Symbol,
                request.PurchasePrice,
                request.Quantity,
                request.PurchaseDate);

            // 6. Enrich session with Correlation ID before saving events
            // This ensures Correlation ID is included in event metadata
            _correlationIdEnricher.EnrichWithCorrelationId(_session);

            // 7. Start event stream for this investment and save events
            // Marten will automatically apply the InvestmentProjection to create InvestmentReadModel
            // Extension method automatically adds OpenTelemetry tracing
            _session.Events.StartStreamWithTracing<InvestmentAggregate>(
                investmentId.Value,
                investmentAggregate.GetUncommittedEvents().ToArray());

            // 8. Save changes to Marten (persist events + update projections)
            // Extension method automatically adds OpenTelemetry tracing
            await _session.SaveChangesWithTracingAsync(cancellationToken);

            // 9. Record business metrics using extension method
            investmentAggregate.RecordMetrics(
                _metricsRecorder,
                m => m.RecordInvestmentAdded(),
                "InvestmentProjection");

            _logger.LogInformation("Successfully added investment {InvestmentId} to portfolio {PortfolioId} with {EventCount} events", 
                investmentId.Value, request.PortfolioId.Value, investmentAggregate.GetUncommittedEvents().Count());

            // 8. Return success
            return AddInvestmentResult.Success(investmentId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Add investment cancelled for portfolio {PortfolioId}", request.PortfolioId.Value);
            // Re-throw cancellation exceptions
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add investment to portfolio {PortfolioId}: {Message}", 
                request.PortfolioId.Value, ex.Message);
            return AddInvestmentResult.Failure($"Failed to add investment: {ex.Message}");
        }
    }
}
