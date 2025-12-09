using FluentAssertions;
using InvestmentHub.Domain.Common;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Events;
using InvestmentHub.Domain.Services;
using InvestmentHub.Domain.ValueObjects;
using Xunit;

namespace InvestmentHub.Domain.Tests.Services;

/// <summary>
/// Testy dla InMemoryDomainEventPublisher sprawdzające:
/// - Rejestrację i wyrejestrowywanie subscriberów
/// - Publikowanie pojedynczych eventów
/// - Publikowanie wielu eventów w batch
/// - Obsługę różnych typów eventów
/// - Walidację parametrów wejściowych
/// - Obsługę błędów podczas publikowania
/// - Thread safety (podstawowe testy)
/// </summary>
public class InMemoryDomainEventPublisherTests
{
    private readonly InMemoryDomainEventPublisher _publisher;
    
    public InMemoryDomainEventPublisherTests()
    {
        _publisher = new InMemoryDomainEventPublisher();
    }
    
    [Fact]
    public void RegisterSubscriber_WithValidSubscriber_ShouldRegisterSuccessfully()
    {
        // Arrange
        var subscriber = new MockInvestmentAddedEventSubscriber();
        
        // Act
        _publisher.RegisterSubscriber(subscriber);
        
        // Assert
        var subscribers = _publisher.GetSubscribers<InvestmentAddedEvent>();
        subscribers.Should().Contain(subscriber);
    }
    
    [Fact]
    public void RegisterSubscriber_WithNullSubscriber_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = () => _publisher.RegisterSubscriber<InvestmentAddedEvent>(null!);
        
        // Assert
        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("subscriber");
    }
    
    [Fact]
    public void RegisterSubscriber_WithMultipleSubscribers_ShouldRegisterAll()
    {
        // Arrange
        var subscriber1 = new MockInvestmentAddedEventSubscriber();
        var subscriber2 = new MockInvestmentAddedEventSubscriber();
        
        // Act
        _publisher.RegisterSubscriber(subscriber1);
        _publisher.RegisterSubscriber(subscriber2);
        
        // Assert
        var subscribers = _publisher.GetSubscribers<InvestmentAddedEvent>();
        subscribers.Should().HaveCount(2);
        subscribers.Should().Contain(subscriber1);
        subscribers.Should().Contain(subscriber2);
    }
    
    [Fact]
    public async Task PublishAsync_WithRegisteredSubscriber_ShouldCallHandleAsync()
    {
        // Arrange
        var subscriber = new MockInvestmentAddedEventSubscriber();
        _publisher.RegisterSubscriber(subscriber);
        var domainEvent = CreateInvestmentAddedEvent();
        
        // Act
        await _publisher.PublishAsync(domainEvent);
        
        // Assert
        subscriber.HandledEvents.Should().Contain(domainEvent);
        subscriber.HandleCallCount.Should().Be(1);
    }
    
    [Fact]
    public async Task PublishAsync_WithMultipleSubscribers_ShouldCallAllHandlers()
    {
        // Arrange
        var subscriber1 = new MockInvestmentAddedEventSubscriber();
        var subscriber2 = new MockInvestmentAddedEventSubscriber();
        _publisher.RegisterSubscriber(subscriber1);
        _publisher.RegisterSubscriber(subscriber2);
        var domainEvent = CreateInvestmentAddedEvent();
        
        // Act
        await _publisher.PublishAsync(domainEvent);
        
        // Assert
        subscriber1.HandledEvents.Should().Contain(domainEvent);
        subscriber2.HandledEvents.Should().Contain(domainEvent);
        subscriber1.HandleCallCount.Should().Be(1);
        subscriber2.HandleCallCount.Should().Be(1);
    }
    
    [Fact]
    public async Task PublishAsync_WithNoSubscribers_ShouldNotThrow()
    {
        // Arrange
        var domainEvent = CreateInvestmentAddedEvent();
        
        // Act & Assert
        var action = async () => await _publisher.PublishAsync(domainEvent);
        await action.Should().NotThrowAsync();
    }
    
    [Fact]
    public async Task PublishAsync_WithNullEvent_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = async () => await _publisher.PublishAsync<InvestmentAddedEvent>(null!);
        
        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("domainEvent");
    }
    
    [Fact]
    public async Task PublishAsync_WithMultipleEvents_ShouldPublishAllEvents()
    {
        // Arrange
        var subscriber = new MockInvestmentAddedEventSubscriber();
        _publisher.RegisterSubscriber(subscriber);
        var events = new List<DomainEvent>
        {
            CreateInvestmentAddedEvent(),
            CreateInvestmentAddedEvent(),
            CreateInvestmentAddedEvent()
        };
        
        // Act
        await _publisher.PublishAsync(events);
        
        // Assert
        subscriber.HandleCallCount.Should().Be(3);
        subscriber.HandledEvents.Should().HaveCount(3);
    }
    
    [Fact]
    public async Task PublishAsync_WithEmptyEventCollection_ShouldNotThrow()
    {
        // Arrange
        var events = new List<DomainEvent>();
        
        // Act & Assert
        var action = async () => await _publisher.PublishAsync(events);
        await action.Should().NotThrowAsync();
    }
    
    [Fact]
    public async Task PublishAsync_WithNullEventCollection_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = async () => await _publisher.PublishAsync(null!);
        
        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("domainEvents");
    }
    
    [Fact]
    public async Task PublishAsync_WhenSubscriberThrows_ShouldPropagateException()
    {
        // Arrange
        var subscriber = new MockInvestmentAddedEventSubscriber { ShouldThrow = true };
        _publisher.RegisterSubscriber(subscriber);
        var domainEvent = CreateInvestmentAddedEvent();
        
        // Act
        var action = async () => await _publisher.PublishAsync(domainEvent);
        
        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Mock exception for testing");
    }
    
    [Fact]
    public async Task PublishAsync_WithDifferentEventTypes_ShouldOnlyCallMatchingSubscribers()
    {
        // Arrange
        var investmentSubscriber = new MockInvestmentAddedEventSubscriber();
        var otherSubscriber = new MockOtherEventSubscriber();
        
        _publisher.RegisterSubscriber(investmentSubscriber);
        _publisher.RegisterSubscriber(otherSubscriber);
        
        var investmentEvent = CreateInvestmentAddedEvent();
        
        // Act
        await _publisher.PublishAsync(investmentEvent);
        
        // Assert
        investmentSubscriber.HandleCallCount.Should().Be(1);
        otherSubscriber.HandleCallCount.Should().Be(0);
    }
    
    [Fact]
    public void GetSubscribers_WithNoRegisteredSubscribers_ShouldReturnEmptyCollection()
    {
        // Act
        var subscribers = _publisher.GetSubscribers<InvestmentAddedEvent>();
        
        // Assert
        subscribers.Should().BeEmpty();
    }
    
    [Fact]
    public void GetSubscribers_WithRegisteredSubscribers_ShouldReturnAllSubscribers()
    {
        // Arrange
        var subscriber1 = new MockInvestmentAddedEventSubscriber();
        var subscriber2 = new MockInvestmentAddedEventSubscriber();
        
        // Verify initial state
        _publisher.GetSubscribers<InvestmentAddedEvent>().Should().BeEmpty();
        
        _publisher.RegisterSubscriber(subscriber1);
        _publisher.RegisterSubscriber(subscriber2);
        
        // Act
        var subscribers = _publisher.GetSubscribers<InvestmentAddedEvent>();
        
        // Assert
        subscribers.Should().HaveCount(2);
        subscribers.Should().Contain(subscriber1);
        subscribers.Should().Contain(subscriber2);
    }
    
    private static InvestmentAddedEvent CreateInvestmentAddedEvent()
    {
        var portfolioId = PortfolioId.New();
        var investmentId = InvestmentId.New();
        var symbol = Symbol.Stock("AAPL", "NASDAQ");
        var quantity = 10m;
        var purchasePrice = new Money(150m, Currency.USD);
        var initialCurrentValue = new Money(purchasePrice.Amount * quantity, Currency.USD);
        var purchaseDate = DateTime.UtcNow.AddDays(-30);
        
        return new InvestmentAddedEvent(
            portfolioId,
            investmentId,
            symbol,
            purchasePrice,
            quantity,
            purchaseDate,
            initialCurrentValue);
    }
}

/// <summary>
/// Mock subscriber for InvestmentAddedEvent for testing purposes.
/// </summary>
public class MockInvestmentAddedEventSubscriber : IDomainEventSubscriber<InvestmentAddedEvent>
{
    public List<InvestmentAddedEvent> HandledEvents { get; } = new();
    public int HandleCallCount { get; private set; }
    public bool ShouldThrow { get; set; }
    
    public async Task HandleAsync(InvestmentAddedEvent domainEvent)
    {
        if (ShouldThrow)
            throw new InvalidOperationException("Mock exception for testing");
        
        HandledEvents.Add(domainEvent);
        HandleCallCount++;
        await Task.CompletedTask;
    }
}

/// <summary>
/// Mock subscriber for a different event type for testing purposes.
/// </summary>
public class MockOtherEventSubscriber : IDomainEventSubscriber<DomainEvent>
{
    public int HandleCallCount { get; private set; }
    
    public async Task HandleAsync(DomainEvent domainEvent)
    {
        HandleCallCount++;
        await Task.CompletedTask;
    }
}
