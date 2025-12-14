using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Repositories;
using MediatR;
using Marten;
using Microsoft.Extensions.Logging;
using InvestmentHub.Domain.Services;
using InvestmentHub.Domain.Common;

namespace InvestmentHub.Domain.Handlers.Commands;

/// <summary>
/// Handler for CreatePortfolioCommand.
/// Responsible for creating a new portfolio using Event Sourcing with Marten.
/// </summary>
public class CreatePortfolioCommandHandler : IRequestHandler<CreatePortfolioCommand, CreatePortfolioResult>
{
    private readonly IDocumentSession _session;
    private readonly IUserRepository _userRepository;
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly ILogger<CreatePortfolioCommandHandler> _logger;
    private readonly ICorrelationIdEnricher _correlationIdEnricher;
    private readonly IMetricsRecorder _metricsRecorder;

    /// <summary>
    /// Initializes a new instance of the CreatePortfolioCommandHandler class.
    /// </summary>
    /// <param name="session">The Marten document session for event sourcing</param>
    /// <param name="userRepository">The user repository</param>
    /// <param name="portfolioRepository">The portfolio repository for validation</param>
    /// <param name="logger">The logger</param>
    /// <param name="correlationIdEnricher">The Correlation ID enricher for Marten sessions</param>
    /// <param name="metricsRecorder">The metrics recorder service</param>
    public CreatePortfolioCommandHandler(
        IDocumentSession session,
        IUserRepository userRepository,
        IPortfolioRepository portfolioRepository,
        ILogger<CreatePortfolioCommandHandler> logger,
        ICorrelationIdEnricher correlationIdEnricher,
        IMetricsRecorder metricsRecorder)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _portfolioRepository = portfolioRepository ?? throw new ArgumentNullException(nameof(portfolioRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _correlationIdEnricher = correlationIdEnricher ?? throw new ArgumentNullException(nameof(correlationIdEnricher));
        _metricsRecorder = metricsRecorder ?? throw new ArgumentNullException(nameof(metricsRecorder));
    }

    /// <summary>
    /// Handles the CreatePortfolioCommand using Event Sourcing.
    /// </summary>
    /// <param name="request">The command request</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The result of the operation</returns>
    public async Task<CreatePortfolioResult> Handle(CreatePortfolioCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating portfolio {PortfolioId} for user {OwnerId}", 
                request.PortfolioId.Value, request.OwnerId.Value);

            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();
            
            // Validate the request
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                _logger.LogWarning("Portfolio name cannot be empty");
                return CreatePortfolioResult.Failure("Portfolio name cannot be empty");
            }

            // 1. Validate user exists
            var user = await _userRepository.GetByIdAsync(request.OwnerId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User {OwnerId} not found", request.OwnerId.Value);
                return CreatePortfolioResult.Failure("User not found");
            }

            // 2. Check if user can create more portfolios
            var canCreate = await _userRepository.CanCreatePortfolioAsync(request.OwnerId, 10, cancellationToken);
            if (!canCreate)
            {
                _logger.LogWarning("User {OwnerId} has reached maximum portfolio limit", request.OwnerId.Value);
                return CreatePortfolioResult.Failure("User has reached the maximum number of portfolios");
            }

            // 3. Check for duplicate portfolio name
            var existsByName = await _portfolioRepository.ExistsByNameAsync(request.OwnerId, request.Name, cancellationToken);
            if (existsByName)
            {
                _logger.LogWarning("Portfolio with name '{Name}' already exists for user {OwnerId}", 
                    request.Name, request.OwnerId.Value);
                return CreatePortfolioResult.Failure($"Portfolio with name '{request.Name}' already exists for this user");
            }

            // 4. Create portfolio aggregate (generates PortfolioCreatedEvent)
            var portfolioAggregate = PortfolioAggregate.Initiate(
                request.PortfolioId,
                request.OwnerId,
                request.Name,
                request.Description,
                request.Currency);

            // 5. Enrich session with Correlation ID before saving events
            // This ensures Correlation ID is included in event metadata
            _correlationIdEnricher.EnrichWithCorrelationId(_session);

            // 6. Start event stream for this portfolio and save events
            // Marten will automatically apply the projection to create PortfolioReadModel
            _session.Events.StartStreamWithTracing<PortfolioAggregate>(
                request.PortfolioId.Value,
                portfolioAggregate.GetUncommittedEvents().ToArray());

            // 7. Save changes to Marten (persist events + update projections)
            await _session.SaveChangesWithTracingAsync(cancellationToken);

            // 8. Record business metrics using extension method
            portfolioAggregate.RecordMetrics(
                _metricsRecorder,
                m => m.RecordPortfolioCreated(),
                "PortfolioProjection");

            _logger.LogInformation("Successfully created portfolio {PortfolioId} with {EventCount} events", 
                request.PortfolioId.Value, portfolioAggregate.GetUncommittedEvents().Count);

            // 7. Return success
            return CreatePortfolioResult.Success(request.PortfolioId);
        }

        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to create portfolio {PortfolioId}: {Message}", 
                request.PortfolioId.Value, ex.Message);
            return CreatePortfolioResult.Failure($"Failed to create portfolio: {ex.Message}");
        }
    }
}
