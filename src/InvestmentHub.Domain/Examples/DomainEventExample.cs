using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.EventHandlers;
using InvestmentHub.Domain.Events;
using InvestmentHub.Domain.Services;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Examples;

/// <summary>
/// Example demonstrating how domain events work in the InvestmentHub system.
/// This class shows the complete flow from adding an investment to handling the resulting event.
/// </summary>
public static class DomainEventExample
{
    /// <summary>
    /// Demonstrates the complete domain event flow.
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    public static async Task DemonstrateDomainEventsAsync()
    {
        Console.WriteLine("=== InvestmentHub Domain Events Demo ===\n");
        
        // 1. Create domain event publisher and register subscribers
        var eventPublisher = new InMemoryDomainEventPublisher();
        var valuationService = new PortfolioValuationService();
        var eventHandler = new InvestmentAddedEventHandler(valuationService);
        
        // Register the event handler
        eventPublisher.RegisterSubscriber(eventHandler);
        
        // 2. Create a portfolio
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        var portfolio = new Portfolio(
            portfolioId,
            "My Investment Portfolio",
            "A diversified portfolio for long-term growth",
            ownerId);
        
        Console.WriteLine($"Created portfolio: {portfolio}");
        Console.WriteLine($"Portfolio ID: {portfolioId.Value}");
        Console.WriteLine($"Owner ID: {ownerId.Value}\n");
        
        // 3. Create an investment
        var investmentId = InvestmentId.New();
        var symbol = Symbol.Stock("AAPL", "NASDAQ");
        var purchasePrice = new Money(150.00m, Currency.USD);
        var quantity = 10m;
        var purchaseDate = DateTime.UtcNow.AddDays(-30);
        
        var investment = new Investment(
            investmentId,
            portfolioId,
            symbol,
            purchasePrice,
            quantity,
            purchaseDate);
        
        Console.WriteLine($"Created investment: {investment}\n");
        
        // 4. Add investment to portfolio (this will raise InvestmentAddedEvent)
        Console.WriteLine("Adding investment to portfolio...");
        portfolio.AddInvestment(investment);
        
        Console.WriteLine($"Investment added successfully!");
        Console.WriteLine($"Portfolio now has {portfolio.GetActiveInvestmentCount()} investments");
        Console.WriteLine($"Total portfolio value: {portfolio.GetTotalValue()}\n");
        
        // 5. Check if domain events were raised
        Console.WriteLine($"Domain events raised: {portfolio.DomainEvents.Count}");
        foreach (var domainEvent in portfolio.DomainEvents)
        {
            Console.WriteLine($"- {domainEvent.GetEventType()}: {domainEvent}");
        }
        Console.WriteLine();
        
        // 6. Publish domain events
        Console.WriteLine("Publishing domain events...");
        await eventPublisher.PublishAsync(portfolio.DomainEvents);
        
        // 7. Clear domain events after processing
        portfolio.ClearDomainEvents();
        Console.WriteLine($"Domain events cleared. Remaining events: {portfolio.DomainEvents.Count}\n");
        
        // 8. Demonstrate portfolio valuation service
        Console.WriteLine("=== Portfolio Valuation Analysis ===");
        var totalValue = await valuationService.CalculateTotalValueAsync(portfolio);
        var totalCost = await valuationService.CalculateTotalCostAsync(portfolio);
        var gainLoss = await valuationService.CalculateUnrealizedGainLossAsync(portfolio);
        var percentageReturn = await valuationService.CalculatePercentageReturnAsync(portfolio);
        
        Console.WriteLine($"Total Value: {totalValue}");
        Console.WriteLine($"Total Cost: {totalCost}");
        Console.WriteLine($"Unrealized Gain/Loss: {gainLoss}");
        Console.WriteLine($"Percentage Return: {percentageReturn:P2}\n");
        
        // 9. Demonstrate diversification analysis
        var diversification = await valuationService.AnalyzeDiversificationAsync(portfolio);
        Console.WriteLine($"Asset Types: {diversification.AssetTypeCount}");
        Console.WriteLine($"Concentration Risk: {diversification.ConcentrationRisk:P2}");
        Console.WriteLine($"Diversification Score: {diversification.DiversificationScore:F1}/100\n");
        
        // 10. Demonstrate risk analysis
        var riskAnalysis = await valuationService.AnalyzeRiskAsync(portfolio);
        Console.WriteLine($"Risk Score: {riskAnalysis.RiskScore:F1}/100");
        Console.WriteLine($"Risk Level: {riskAnalysis.RiskLevel}\n");
        
        Console.WriteLine("=== Demo Complete ===");
    }
}
