using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.ValueObjects;
using MediatR;
using Marten;
using Microsoft.Extensions.Logging;
using InvestmentHub.Domain.Services;
using InvestmentHub.Domain.Common;

namespace InvestmentHub.Domain.Handlers.Commands;

/// <summary>
/// Handler for UpdateInvestmentValueCommand.
/// Responsible for updating an investment's current market value using Event Sourcing.
/// </summary>
public class UpdateInvestmentValueCommandHandler : IRequestHandler<UpdateInvestmentValueCommand, UpdateInvestmentValueResult>
{
    private readonly IDocumentSession _session;
    private readonly ILogger<UpdateInvestmentValueCommandHandler> _logger;
    private readonly ICorrelationIdEnricher _correlationIdEnricher;

    /// <summary>
    /// Initializes a new instance of the UpdateInvestmentValueCommandHandler class.
    /// </summary>
    /// <param name="session">The Marten document session for event sourcing</param>
    /// <param name="logger">The logger</param>
    /// <param name="correlationIdEnricher">The Correlation ID enricher for Marten sessions</param>
    public UpdateInvestmentValueCommandHandler(
        IDocumentSession session,
        ILogger<UpdateInvestmentValueCommandHandler> logger,
        ICorrelationIdEnricher correlationIdEnricher)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _correlationIdEnricher = correlationIdEnricher ?? throw new ArgumentNullException(nameof(correlationIdEnricher));
    }

    /// <summary>
    /// Handles the UpdateInvestmentValueCommand using Event Sourcing.
    /// </summary>
    /// <param name="request">The command request</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The result of the operation</returns>
    public async Task<UpdateInvestmentValueResult> Handle(UpdateInvestmentValueCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating investment value for {InvestmentId} to {NewPrice} {Currency}", 
                request.InvestmentId.Value, request.CurrentPrice.Amount, request.CurrentPrice.Currency);

            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();

            // 1. Load events from the investment stream
            // Extension method automatically adds OpenTelemetry tracing
            var events = await _session.Events.FetchStreamWithTracingAsync(request.InvestmentId.Value, cancellationToken);
            
            if (!events.Any())
            {
                _logger.LogWarning("Investment {InvestmentId} not found in event stream", request.InvestmentId.Value);
                return UpdateInvestmentValueResult.Failure("Investment not found");
            }

            // 2. Reconstruct aggregate from events
            var investmentAggregate = new InvestmentAggregate();
            foreach (var evt in events)
            {
                ((dynamic)investmentAggregate).Apply((dynamic)evt.Data);
            }
            investmentAggregate.ClearUncommittedEvents();

            // 3. Update the current value (generates InvestmentValueUpdatedEvent)
            // CurrentPrice is per unit, so we pass it directly
            investmentAggregate.UpdateValue(request.CurrentPrice);

            // 4. Enrich session with Correlation ID before saving events
            // This ensures Correlation ID is included in event metadata
            _correlationIdEnricher.EnrichWithCorrelationId(_session);

            // 5. Append new events to the stream
            // Extension method automatically adds OpenTelemetry tracing
            _session.Events.AppendWithTracing(
                request.InvestmentId.Value,
                investmentAggregate.GetUncommittedEvents().ToArray());

            // 6. Save changes to Marten (persist events + update projections)
            // Extension method automatically adds OpenTelemetry tracing
            await _session.SaveChangesWithTracingAsync(cancellationToken);

            _logger.LogInformation("Successfully updated investment {InvestmentId}. New total value: {CurrentValue} {Currency}", 
                request.InvestmentId.Value, investmentAggregate.CurrentValue.Amount, investmentAggregate.CurrentValue.Currency);

            // 6. Return success with the new total value
            return UpdateInvestmentValueResult.Success(investmentAggregate.CurrentValue);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Update investment value cancelled for {InvestmentId}", request.InvestmentId.Value);
            // Re-throw cancellation exceptions
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot update investment {InvestmentId}: {Message}", 
                request.InvestmentId.Value, ex.Message);
            return UpdateInvestmentValueResult.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update investment value for {InvestmentId}: {Message}", 
                request.InvestmentId.Value, ex.Message);
            return UpdateInvestmentValueResult.Failure($"Failed to update investment value: {ex.Message}");
        }
    }
}
