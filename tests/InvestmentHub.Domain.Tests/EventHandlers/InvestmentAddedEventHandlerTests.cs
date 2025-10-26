using FluentAssertions;
using InvestmentHub.Domain.Common;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.EventHandlers;
using InvestmentHub.Domain.Events;
using InvestmentHub.Domain.Services;
using InvestmentHub.Domain.ValueObjects;
using Xunit;

namespace InvestmentHub.Domain.Tests.EventHandlers;

/// <summary>
/// Testy dla InvestmentAddedEventHandler sprawdzające:
/// - Poprawne obsługiwanie InvestmentAddedEvent
/// - Integrację z PortfolioValuationService
/// - Obsługę błędów podczas przetwarzania eventów
/// - Walidację parametrów konstruktora
/// - Logowanie i śledzenie operacji
/// </summary>
public class InvestmentAddedEventHandlerTests
{
    private readonly MockPortfolioValuationService _mockValuationService;
    private readonly InvestmentAddedEventHandler _eventHandler;
    
    public InvestmentAddedEventHandlerTests()
    {
        _mockValuationService = new MockPortfolioValuationService();
        _eventHandler = new InvestmentAddedEventHandler(_mockValuationService);
    }
    
    [Fact]
    public void Constructor_WithValidValuationService_ShouldCreateHandler()
    {
        // Arrange & Act
        var handler = new InvestmentAddedEventHandler(_mockValuationService);
        
        // Assert
        handler.Should().NotBeNull();
    }
    
    [Fact]
    public void Constructor_WithNullValuationService_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = () => new InvestmentAddedEventHandler(null!);
        
        // Assert
        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("valuationService");
    }
    
    [Fact]
    public async Task HandleAsync_WithValidEvent_ShouldProcessEventSuccessfully()
    {
        // Arrange
        var investmentAddedEvent = CreateInvestmentAddedEvent();
        
        // Act
        await _eventHandler.HandleAsync(investmentAddedEvent);
        
        // Assert
        _mockValuationService.LastProcessedEvent.Should().Be(investmentAddedEvent);
        _mockValuationService.ProcessEventCallCount.Should().Be(1);
    }
    
    [Fact]
    public async Task HandleAsync_WithNullEvent_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = async () => await _eventHandler.HandleAsync(null!);
        
        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task HandleAsync_WithMultipleEvents_ShouldProcessEachEvent()
    {
        // Arrange
        var event1 = CreateInvestmentAddedEvent();
        var event2 = CreateInvestmentAddedEvent();
        
        // Act
        await _eventHandler.HandleAsync(event1);
        await _eventHandler.HandleAsync(event2);
        
        // Assert
        _mockValuationService.ProcessEventCallCount.Should().Be(2);
        _mockValuationService.LastProcessedEvent.Should().Be(event2);
    }
    
    [Fact]
    public async Task HandleAsync_WhenValuationServiceThrows_ShouldPropagateException()
    {
        // Arrange
        _mockValuationService.ShouldThrowException = true;
        var investmentAddedEvent = CreateInvestmentAddedEvent();
        
        // Act
        var action = async () => await _eventHandler.HandleAsync(investmentAddedEvent);
        
        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Mock exception for testing");
    }
    
    private InvestmentAddedEvent CreateInvestmentAddedEvent()
    {
        var portfolioId = PortfolioId.New();
        var investmentId = InvestmentId.New();
        var symbol = Symbol.Stock("AAPL", "NASDAQ");
        var quantity = 10m;
        var purchasePrice = new Money(150m, Currency.USD);
        var totalCost = purchasePrice.Multiply(quantity);
        var purchaseDate = DateTime.UtcNow.AddDays(-30);
        var ownerId = UserId.New();
        
        return new InvestmentAddedEvent(
            portfolioId,
            investmentId,
            symbol,
            quantity,
            purchasePrice,
            totalCost,
            purchaseDate,
            ownerId);
    }
}

/// <summary>
/// Mock implementation of IPortfolioValuationService for testing purposes.
/// </summary>
public class MockPortfolioValuationService : IPortfolioValuationService
{
    public InvestmentAddedEvent? LastProcessedEvent { get; private set; }
    public int ProcessEventCallCount { get; private set; }
    public bool ShouldThrowException { get; set; }
    
    public async Task<Money> CalculateTotalValueAsync(Portfolio portfolio)
    {
        await Task.CompletedTask;
        return Money.Zero(Currency.USD);
    }
    
    public async Task<Money> CalculateTotalCostAsync(Portfolio portfolio)
    {
        await Task.CompletedTask;
        return Money.Zero(Currency.USD);
    }
    
    public async Task<Money> CalculateUnrealizedGainLossAsync(Portfolio portfolio)
    {
        await Task.CompletedTask;
        return Money.Zero(Currency.USD);
    }
    
    public async Task<decimal> CalculatePercentageReturnAsync(Portfolio portfolio)
    {
        await Task.CompletedTask;
        return 0m;
    }
    
    public async Task<PortfolioDiversificationAnalysis> AnalyzeDiversificationAsync(Portfolio portfolio)
    {
        await Task.CompletedTask;
        return new PortfolioDiversificationAnalysis(0, 0, 0, 0);
    }
    
    public async Task<IEnumerable<InvestmentPerformance>> GetTopPerformersAsync(Portfolio portfolio, int topCount = 5)
    {
        await Task.CompletedTask;
        return Enumerable.Empty<InvestmentPerformance>();
    }
    
    public async Task<IEnumerable<InvestmentPerformance>> GetWorstPerformersAsync(Portfolio portfolio, int bottomCount = 5)
    {
        await Task.CompletedTask;
        return Enumerable.Empty<InvestmentPerformance>();
    }
    
    public async Task<PortfolioRiskAnalysis> AnalyzeRiskAsync(Portfolio portfolio)
    {
        await Task.CompletedTask;
        return new PortfolioRiskAnalysis(0, 0, 0, RiskLevel.VeryLow);
    }
    
    public async Task ProcessEvent(InvestmentAddedEvent eventData)
    {
        if (ShouldThrowException)
            throw new InvalidOperationException("Mock exception for testing");
        
        await Task.CompletedTask;
        LastProcessedEvent = eventData;
        ProcessEventCallCount++;
    }
}
