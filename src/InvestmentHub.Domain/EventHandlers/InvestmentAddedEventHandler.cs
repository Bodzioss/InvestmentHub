using InvestmentHub.Domain.Common;
using InvestmentHub.Domain.Events;
using InvestmentHub.Domain.Services;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Domain.EventHandlers;

/// <summary>
/// Event handler that responds to InvestmentAddedEvent by updating portfolio valuation.
/// This handler demonstrates how domain events can trigger business logic updates.
/// </summary>
public class InvestmentAddedEventHandler : IDomainEventSubscriber<InvestmentAddedEvent>
{
    private readonly IPortfolioValuationService _valuationService;
    private readonly ILogger<InvestmentAddedEventHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the InvestmentAddedEventHandler class.
    /// </summary>
    /// <param name="valuationService">The portfolio valuation service</param>
    /// <param name="logger">The logger</param>
    /// <exception cref="ArgumentNullException">Thrown when dependencies are null</exception>
    public InvestmentAddedEventHandler(IPortfolioValuationService valuationService, ILogger<InvestmentAddedEventHandler> logger)
    {
        _valuationService = valuationService ?? throw new ArgumentNullException(nameof(valuationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the InvestmentAddedEvent by updating portfolio valuation and analytics.
    /// </summary>
    /// <param name="domainEvent">The InvestmentAddedEvent to handle</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task HandleAsync(InvestmentAddedEvent domainEvent)
    {
        if (domainEvent == null)
            throw new ArgumentNullException(nameof(domainEvent));

        try
        {
            _logger.LogInformation("Handling InvestmentAddedEvent for Portfolio {PortfolioId}, Ticker {Ticker}",
                domainEvent.PortfolioId.Value, domainEvent.Symbol.Ticker);

            // Update portfolio valuation using the service
            await _valuationService.ProcessEvent(domainEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling InvestmentAddedEvent for Portfolio {PortfolioId}: {Message}",
                domainEvent.PortfolioId.Value, ex.Message);
            throw;
        }
    }

}
