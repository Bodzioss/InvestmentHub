using InvestmentHub.Domain.Common;
using InvestmentHub.Domain.Events;
using InvestmentHub.Domain.Services;

namespace InvestmentHub.Domain.EventHandlers;

/// <summary>
/// Event handler that responds to InvestmentAddedEvent by updating portfolio valuation.
/// This handler demonstrates how domain events can trigger business logic updates.
/// </summary>
public class InvestmentAddedEventHandler : IDomainEventSubscriber<InvestmentAddedEvent>
{
    private readonly IPortfolioValuationService _valuationService;
    
    /// <summary>
    /// Initializes a new instance of the InvestmentAddedEventHandler class.
    /// </summary>
    /// <param name="valuationService">The portfolio valuation service</param>
    /// <exception cref="ArgumentNullException">Thrown when valuationService is null</exception>
    public InvestmentAddedEventHandler(IPortfolioValuationService valuationService)
    {
        _valuationService = valuationService ?? throw new ArgumentNullException(nameof(valuationService));
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
            // Log the event (in a real application, this would use proper logging)
            Console.WriteLine($"Handling InvestmentAddedEvent: {domainEvent}");         
            
            // Update portfolio valuation using the service
            await _valuationService.ProcessEvent(domainEvent);
            
            // For now, we'll simulate additional valuation update
            await SimulateValuationUpdateAsync(domainEvent);
        }
        catch (Exception ex)
        {
            // In a real application, this would use proper error handling and logging
            Console.WriteLine($"Error handling InvestmentAddedEvent: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Simulates portfolio valuation update after investment addition.
    /// In a real application, this would interact with repositories and external services.
    /// </summary>
    /// <param name="domainEvent">The InvestmentAddedEvent</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task SimulateValuationUpdateAsync(InvestmentAddedEvent domainEvent)
    {
        // Simulate async work
        await Task.Delay(100);        
        
        Console.WriteLine($"Portfolio valuation updated for Portfolio {domainEvent.PortfolioId.Value}");
        Console.WriteLine($"Added investment: {domainEvent.Symbol.Ticker} worth {domainEvent.TotalCost}");
    }
}
