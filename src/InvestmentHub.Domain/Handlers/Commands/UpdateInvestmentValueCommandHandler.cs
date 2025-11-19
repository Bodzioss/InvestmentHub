using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.ValueObjects;
using MediatR;
using Marten;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Domain.Handlers.Commands;

/// <summary>
/// Handler for UpdateInvestmentValueCommand.
/// Responsible for updating an investment's current market value using Event Sourcing.
/// </summary>
public class UpdateInvestmentValueCommandHandler : IRequestHandler<UpdateInvestmentValueCommand, UpdateInvestmentValueResult>
{
    private readonly IDocumentSession _session;
    private readonly ILogger<UpdateInvestmentValueCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the UpdateInvestmentValueCommandHandler class.
    /// </summary>
    /// <param name="session">The Marten document session for event sourcing</param>
    /// <param name="logger">The logger</param>
    public UpdateInvestmentValueCommandHandler(
        IDocumentSession session,
        ILogger<UpdateInvestmentValueCommandHandler> logger)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            var events = await _session.Events.FetchStreamAsync(request.InvestmentId.Value, token: cancellationToken);
            
            if (events == null || !events.Any())
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

            // 4. Append new events to the stream
            _session.Events.Append(
                request.InvestmentId.Value,
                investmentAggregate.GetUncommittedEvents().ToArray());

            // 5. Save changes to Marten (persist events + update projections)
            await _session.SaveChangesAsync(cancellationToken);

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
