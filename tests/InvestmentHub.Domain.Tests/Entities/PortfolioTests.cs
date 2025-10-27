using FluentAssertions;
using InvestmentHub.Domain.Common;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Events;
using InvestmentHub.Domain.ValueObjects;
using Xunit;

namespace InvestmentHub.Domain.Tests.Entities;

/// <summary>
/// Testy dla Portfolio aggregate sprawdzające:
/// - Invarianty biznesowe (nazwa nie może być pusta, nie można dodać duplikatów)
/// - Zarządzanie kolekcją inwestycji (dodawanie, usuwanie, wyszukiwanie)
/// - Obliczenia finansowe (GetTotalValue, GetTotalCost, GetTotalGainLoss)
/// - Zarządzanie statusem (Active, Archived, Suspended)
/// - Domain events (InvestmentAddedEvent po dodaniu inwestycji)
/// - Walidację operacji na nieaktywnych portfelach
/// </summary>
public class PortfolioTests
{
    private readonly PortfolioId _portfolioId;
    private readonly UserId _ownerId;
    private readonly string _portfolioName;
    private readonly string _portfolioDescription;
    
    public PortfolioTests()
    {
        _portfolioId = PortfolioId.New();
        _ownerId = UserId.New();
        _portfolioName = "Test Portfolio";
        _portfolioDescription = "Test Description";
    }
    
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreatePortfolio()
    {
        // Arrange & Act
        var portfolio = new Portfolio(_portfolioId, _portfolioName, _portfolioDescription, _ownerId);
        
        // Assert
        portfolio.Id.Should().Be(_portfolioId);
        portfolio.Name.Should().Be(_portfolioName);
        portfolio.Description.Should().Be(_portfolioDescription);
        portfolio.OwnerId.Should().Be(_ownerId);
        portfolio.Status.Should().Be(PortfolioStatus.Active);
        portfolio.Investments.Should().BeEmpty();
        portfolio.DomainEvents.Should().BeEmpty();
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidName_ShouldThrowArgumentException(string name)
    {
        // Arrange & Act
        var action = () => new Portfolio(_portfolioId, name, _portfolioDescription, _ownerId);
        
        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Portfolio name cannot be null or empty*")
            .And.ParamName.Should().Be("name");
    }
    
    [Fact]
    public void Constructor_WithNullId_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = () => new Portfolio(null!, _portfolioName, _portfolioDescription, _ownerId);
        
        // Assert
        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("id");
    }
    
    [Fact]
    public void Constructor_WithNullOwnerId_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = () => new Portfolio(_portfolioId, _portfolioName, _portfolioDescription, null!);
        
        // Assert
        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("ownerId");
    }
    
    [Fact]
    public void AddInvestment_WithValidInvestment_ShouldAddInvestmentAndRaiseEvent()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _portfolioName, _portfolioDescription, _ownerId);
        var investment = CreateTestInvestment();
        
        // Act
        portfolio.AddInvestment(investment);
        
        // Assert
        portfolio.Investments.Should().Contain(investment);
        portfolio.GetActiveInvestmentCount().Should().Be(1);
        portfolio.DomainEvents.Should().HaveCount(1);
        
        var domainEvent = portfolio.DomainEvents.First() as InvestmentAddedEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.PortfolioId.Should().Be(_portfolioId);
        domainEvent.InvestmentId.Should().Be(investment.Id);
        domainEvent.Symbol.Should().Be(investment.Symbol);
    }
    
    [Fact]
    public void AddInvestment_WithDuplicateSymbol_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _portfolioName, _portfolioDescription, _ownerId);
        var investment1 = CreateTestInvestment();
        var investment2 = CreateTestInvestment(); // Same symbol
        
        portfolio.AddInvestment(investment1);
        
        // Act
        var action = () => portfolio.AddInvestment(investment2);
        
        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"Investment with symbol {investment2.Symbol.Ticker} already exists in portfolio*");
    }
    
    [Fact]
    public void AddInvestment_OnArchivedPortfolio_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _portfolioName, _portfolioDescription, _ownerId);
        portfolio.Archive();
        var investment = CreateTestInvestment();
        
        // Act
        var action = () => portfolio.AddInvestment(investment);
        
        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot add investments to inactive portfolio*");
    }
    
    [Fact]
    public void RemoveInvestment_WithExistingInvestment_ShouldRemoveInvestment()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _portfolioName, _portfolioDescription, _ownerId);
        var investment = CreateTestInvestment();
        portfolio.AddInvestment(investment);
        
        // Act
        portfolio.RemoveInvestment(investment.Id);
        
        // Assert
        portfolio.Investments.Should().NotContain(investment);
        portfolio.GetActiveInvestmentCount().Should().Be(0);
    }
    
    [Fact]
    public void RemoveInvestment_WithNonExistentInvestment_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _portfolioName, _portfolioDescription, _ownerId);
        var nonExistentId = InvestmentId.New();
        
        // Act
        var action = () => portfolio.RemoveInvestment(nonExistentId);
        
        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"Investment with ID {nonExistentId.Value} not found in portfolio*");
    }
    
    [Fact]
    public void RemoveInvestment_OnArchivedPortfolio_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _portfolioName, _portfolioDescription, _ownerId);
        var investment = CreateTestInvestment();
        portfolio.AddInvestment(investment);
        portfolio.Archive();
        
        // Act
        var action = () => portfolio.RemoveInvestment(investment.Id);
        
        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot remove investments from inactive portfolio*");
    }
    
    [Fact]
    public void GetInvestment_WithExistingId_ShouldReturnInvestment()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _portfolioName, _portfolioDescription, _ownerId);
        var investment = CreateTestInvestment();
        portfolio.AddInvestment(investment);
        
        // Act
        var result = portfolio.GetInvestment(investment.Id);
        
        // Assert
        result.Should().Be(investment);
    }
    
    [Fact]
    public void GetInvestment_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _portfolioName, _portfolioDescription, _ownerId);
        var nonExistentId = InvestmentId.New();
        
        // Act
        var result = portfolio.GetInvestment(nonExistentId);
        
        // Assert
        result.Should().BeNull();
    }
    
    [Fact]
    public void GetInvestmentBySymbol_WithExistingSymbol_ShouldReturnInvestment()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _portfolioName, _portfolioDescription, _ownerId);
        var investment = CreateTestInvestment();
        portfolio.AddInvestment(investment);
        
        // Act
        var result = portfolio.GetInvestmentBySymbol(investment.Symbol);
        
        // Assert
        result.Should().Be(investment);
    }
    
    [Fact]
    public void GetInvestmentBySymbol_WithNonExistentSymbol_ShouldReturnNull()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _portfolioName, _portfolioDescription, _ownerId);
        var nonExistentSymbol = Symbol.Stock("MSFT", "NASDAQ");
        
        // Act
        var result = portfolio.GetInvestmentBySymbol(nonExistentSymbol);
        
        // Assert
        result.Should().BeNull();
    }
    
    [Fact]
    public void GetTotalValue_WithNoInvestments_ShouldReturnZero()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _portfolioName, _portfolioDescription, _ownerId);
        
        // Act
        var totalValue = portfolio.GetTotalValue();
        
        // Assert
        totalValue.Amount.Should().Be(0m);
        totalValue.Currency.Should().Be(Currency.USD);
    }
    
    [Fact]
    public void GetTotalValue_WithInvestments_ShouldReturnSumOfCurrentValues()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _portfolioName, _portfolioDescription, _ownerId);
        var investment1 = CreateTestInvestment();
        var investment2 = CreateTestInvestmentWithSymbol("MSFT", "NASDAQ");
        
        portfolio.AddInvestment(investment1);
        portfolio.AddInvestment(investment2);
        
        // Act
        var totalValue = portfolio.GetTotalValue();
        
        // Assert
        var expectedValue = investment1.CurrentValue.Add(investment2.CurrentValue);
        totalValue.Should().Be(expectedValue);
    }
    
    [Fact]
    public void GetTotalCost_WithInvestments_ShouldReturnSumOfTotalCosts()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _portfolioName, _portfolioDescription, _ownerId);
        var investment1 = CreateTestInvestment();
        var investment2 = CreateTestInvestmentWithSymbol("MSFT", "NASDAQ");
        
        portfolio.AddInvestment(investment1);
        portfolio.AddInvestment(investment2);
        
        // Act
        var totalCost = portfolio.GetTotalCost();
        
        // Assert
        var expectedCost = investment1.GetTotalCost().Add(investment2.GetTotalCost());
        totalCost.Should().Be(expectedCost);
    }
    
    [Fact]
    public void GetTotalGainLoss_WithProfitableInvestments_ShouldReturnPositiveGain()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _portfolioName, _portfolioDescription, _ownerId);
        var investment = CreateTestInvestment();
        investment.UpdateCurrentValue(new Money(160m, Currency.USD)); // 10% gain
        portfolio.AddInvestment(investment);
        
        // Act
        var gainLoss = portfolio.GetTotalGainLoss();
        
        // Assert
        gainLoss.Amount.Should().BePositive();
    }
    
    [Fact]
    public void UpdateDetails_WithValidParameters_ShouldUpdateDetails()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _portfolioName, _portfolioDescription, _ownerId);
        var newName = "Updated Portfolio";
        var newDescription = "Updated Description";
        
        // Act
        portfolio.UpdateDetails(newName, newDescription);
        
        // Assert
        portfolio.Name.Should().Be(newName);
        portfolio.Description.Should().Be(newDescription);
        portfolio.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public void UpdateDetails_OnArchivedPortfolio_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _portfolioName, _portfolioDescription, _ownerId);
        portfolio.Archive();
        
        // Act
        var action = () => portfolio.UpdateDetails("New Name", "New Description");
        
        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot update inactive portfolio*");
    }
    
    [Fact]
    public void Archive_ShouldChangeStatusToArchived()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _portfolioName, _portfolioDescription, _ownerId);
        
        // Act
        portfolio.Archive();
        
        // Assert
        portfolio.Status.Should().Be(PortfolioStatus.Archived);
        portfolio.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public void Suspend_ShouldChangeStatusToSuspended()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _portfolioName, _portfolioDescription, _ownerId);
        
        // Act
        portfolio.Suspend();
        
        // Assert
        portfolio.Status.Should().Be(PortfolioStatus.Suspended);
        portfolio.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public void Reactivate_OnSuspendedPortfolio_ShouldChangeStatusToActive()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _portfolioName, _portfolioDescription, _ownerId);
        portfolio.Suspend();
        
        // Act
        portfolio.Reactivate();
        
        // Assert
        portfolio.Status.Should().Be(PortfolioStatus.Active);
        portfolio.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public void Reactivate_OnActivePortfolio_ShouldNotChangeStatus()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _portfolioName, _portfolioDescription, _ownerId);
        var originalStatus = portfolio.Status;
        
        // Act
        portfolio.Reactivate();
        
        // Assert
        portfolio.Status.Should().Be(originalStatus);
    }
    
    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _portfolioName, _portfolioDescription, _ownerId);
        var investment = CreateTestInvestment();
        portfolio.AddInvestment(investment);
        
        portfolio.DomainEvents.Should().HaveCount(1);
        
        // Act
        portfolio.ClearDomainEvents();
        
        // Assert
        portfolio.DomainEvents.Should().BeEmpty();
    }
    
    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _portfolioName, _portfolioDescription, _ownerId);
        
        // Act
        var result = portfolio.ToString();
        
        // Assert
        result.Should().Contain(_portfolioName);
        result.Should().Contain(_portfolioId.Value.ToString());
        result.Should().Contain("0 investments");
        result.Should().Contain("Active");
    }
    
    private Investment CreateTestInvestment()
    {
        return CreateTestInvestmentWithSymbol("AAPL", "NASDAQ");
    }
    
    private Investment CreateTestInvestmentWithSymbol(string ticker, string exchange)
    {
        var investmentId = InvestmentId.New();
        var symbol = Symbol.Stock(ticker, exchange);
        var purchasePrice = new Money(150.00m, Currency.USD);
        var quantity = 10m;
        var purchaseDate = DateTime.UtcNow.AddDays(-30);
        
        return new Investment(investmentId, PortfolioId.New(), symbol, purchasePrice, quantity, purchaseDate);
    }
}
