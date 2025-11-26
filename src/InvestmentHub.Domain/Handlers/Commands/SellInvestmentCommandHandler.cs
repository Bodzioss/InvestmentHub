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
/// Handler for SellInvestmentCommand.
/// Responsible for selling an investment (fully or partially) using Event Sourcing.
/// </summary>
public class SellInvestmentCommandHandler : IRequestHandler<SellInvestmentCommand, SellInvestmentResult>
{
    private readonly IDocumentSession _session;
    private readonly ILogger<SellInvestmentCommandHandler> _logger;
    private readonly ICorrelationIdEnricher _correlationIdEnricher;
    private readonly IMetricsRecorder _metricsRecorder;

    /// <summary>
    /// Initializes a new instance of the SellInvestmentCommandHandler class.
    /// </summary>
    /// <param name="session">The Marten document session for event sourcing</param>
    /// <param name="logger">The logger</param>
    /// <param name="correlationIdEnricher">The Correlation ID enricher for Marten sessions</param>
    /// <param name="metricsRecorder">The metrics recorder service</param>
    public SellInvestmentCommandHandler(
        IDocumentSession session,
        ILogger<SellInvestmentCommandHandler> logger,
        ICorrelationIdEnricher correlationIdEnricher,
        IMetricsRecorder metricsRecorder)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _correlationIdEnricher = correlationIdEnricher ?? throw new ArgumentNullException(nameof(correlationIdEnricher));
        _metricsRecorder = metricsRecorder ?? throw new ArgumentNullException(nameof(metricsRecorder));
    }

    /// <summary>
    /// Handles the SellInvestmentCommand using Event Sourcing.
    /// </summary>
    /// <param name="request">The command request</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The result of the operation</returns>
    public async Task<SellInvestmentResult> Handle(SellInvestmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Selling investment {InvestmentId} at {SalePrice} {Currency}. Quantity requested: {QuantityRequested}", 
                request.InvestmentId.Value, 
                request.SalePrice.Amount, 
                request.SalePrice.Currency,
                request.QuantityToSell?.ToString() ?? "ALL");

            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();
            
            // DEBUG: Log the exact quantity value
            _logger.LogInformation("DEBUG: QuantityToSell value = {Value}, HasValue = {HasValue}", 
                request.QuantityToSell, 
                request.QuantityToSell.HasValue);

            // Validate the request
            if (request.SalePrice.Amount < 0)
            {
                _logger.LogWarning("Invalid sale price: {SalePrice}", request.SalePrice.Amount);
                return SellInvestmentResult.Failure("Sale price cannot be negative");
            }

            if (request.QuantityToSell.HasValue && request.QuantityToSell.Value <= 0)
            {
                _logger.LogWarning("Invalid quantity: {Quantity}", request.QuantityToSell.Value);
                return SellInvestmentResult.Failure("Quantity to sell must be greater than zero");
            }

            if (request.SaleDate > DateTime.UtcNow)
            {
                _logger.LogWarning("Invalid sale date: {SaleDate}", request.SaleDate);
                return SellInvestmentResult.Failure("Sale date cannot be in the future");
            }

            // 1. Load events from the investment stream
            // Extension method automatically adds OpenTelemetry tracing
            var events = await _session.Events.FetchStreamWithTracingAsync(request.InvestmentId.Value, cancellationToken);
            
            if (!events.Any())
            {
                _logger.LogWarning("Investment {InvestmentId} not found in event stream", request.InvestmentId.Value);
                return SellInvestmentResult.Failure("Investment not found");
            }

            // 2. Reconstruct aggregate from events
            var investmentAggregate = new InvestmentAggregate();
            foreach (var evt in events)
            {
                ((dynamic)investmentAggregate).Apply((dynamic)evt.Data);
            }
            investmentAggregate.ClearUncommittedEvents();

            // DEBUG: Log aggregate state before sell
            _logger.LogInformation("DEBUG: Before sell - Aggregate Quantity = {Quantity}, Status = {Status}", 
                investmentAggregate.Quantity, 
                investmentAggregate.Status);

            // 3. Sell the investment (generates InvestmentSoldEvent)
            investmentAggregate.Sell(request.SalePrice, request.QuantityToSell, request.SaleDate);
            
            // DEBUG: Log aggregate state after sell
            _logger.LogInformation("DEBUG: After sell - Aggregate Quantity = {Quantity}, Status = {Status}", 
                investmentAggregate.Quantity, 
                investmentAggregate.Status);

            // 4. Get the generated event to extract sale details
            var soldEvent = investmentAggregate.GetUncommittedEvents()
                .OfType<Events.InvestmentSoldEvent>()
                .FirstOrDefault();

            if (soldEvent == null)
            {
                _logger.LogError("InvestmentSoldEvent was not generated by aggregate");
                return SellInvestmentResult.Failure("Failed to generate sale event");
            }

            // 5. Enrich session with Correlation ID before saving events
            // This ensures Correlation ID is included in event metadata
            _correlationIdEnricher.EnrichWithCorrelationId(_session);

            // 6. Append new events to the stream
            // Extension method automatically adds OpenTelemetry tracing
            _session.Events.AppendWithTracing(
                request.InvestmentId.Value,
                investmentAggregate.GetUncommittedEvents().ToArray());

            // 7. Save changes to Marten (persist events + update projections)
            // Extension method automatically adds OpenTelemetry tracing
            await _session.SaveChangesWithTracingAsync(cancellationToken);

            // 8. Record business metrics using extension method
            investmentAggregate.RecordMetrics(
                _metricsRecorder,
                m => m.RecordInvestmentSold(),
                "InvestmentProjection");

            _logger.LogInformation(
                "Successfully sold investment {InvestmentId}. Quantity: {QuantitySold}, P/L: {ProfitLoss} {Currency}, Complete sale: {IsComplete}", 
                request.InvestmentId.Value, 
                soldEvent.QuantitySold,
                soldEvent.RealizedProfitLoss.Amount,
                soldEvent.RealizedProfitLoss.Currency,
                soldEvent.IsCompleteSale);

            // 7. Return success with sale details
            return SellInvestmentResult.Success(
                soldEvent.RealizedProfitLoss, 
                soldEvent.QuantitySold, 
                soldEvent.IsCompleteSale);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Sell investment cancelled for {InvestmentId}", request.InvestmentId.Value);
            // Re-throw cancellation exceptions
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot sell investment {InvestmentId}: {Message}", 
                request.InvestmentId.Value, ex.Message);
            return SellInvestmentResult.Failure(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid arguments for selling investment {InvestmentId}: {Message}", 
                request.InvestmentId.Value, ex.Message);
            return SellInvestmentResult.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sell investment {InvestmentId}: {Message}", 
                request.InvestmentId.Value, ex.Message);
            return SellInvestmentResult.Failure($"Failed to sell investment: {ex.Message}");
        }
    }
}

