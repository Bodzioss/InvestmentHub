using FluentAssertions;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.EventHandlers;
using InvestmentHub.Domain.Events;
using InvestmentHub.Domain.Services;
using InvestmentHub.Domain.ValueObjects;
using Xunit;

namespace InvestmentHub.Domain.Tests.Integration;

/// <summary>
/// Testy integracyjne sprawdzające pełny przepływ domenowy:
/// - Tworzenie portfela i inwestycji
/// - Dodawanie inwestycji do portfela (z eventami)
/// - Publikowanie i obsługę domain events
/// - Obliczenia wyceny portfela
/// - Walidację invariantów biznesowych
/// - Obsługę błędów i edge cases
/// </summary>
public class DomainIntegrationTests
{
    [Fact]
    public async Task CompleteInvestmentFlow_ShouldWorkEndToEnd()
    {
        // Arrange - Setup domain event infrastructure
        var eventPublisher = new InMemoryDomainEventPublisher();
        var valuationService = new PortfolioValuationService();
        var eventHandler = new InvestmentAddedEventHandler(valuationService);
        eventPublisher.RegisterSubscriber(eventHandler);
        
        // Create portfolio
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        var portfolio = new Portfolio(portfolioId, "Test Portfolio", "Integration test portfolio", ownerId);
        
        // Create investments
        var investment1 = CreateInvestment("AAPL", "NASDAQ", AssetType.Stock, 150m, 10m);
        var investment2 = CreateInvestment("MSFT", "NASDAQ", AssetType.Stock, 200m, 5m);
        
        // Act - Add investments to portfolio
        portfolio.AddInvestment(investment1);
        portfolio.AddInvestment(investment2);
        
        // Update investment values to simulate market changes
        investment1.UpdateCurrentValue(new Money(160m, Currency.USD)); // 6.67% gain
        investment2.UpdateCurrentValue(new Money(190m, Currency.USD)); // -5% loss
        
        // Publish domain events
        await eventPublisher.PublishAsync(portfolio.DomainEvents);
        
        // Clear events after processing
        portfolio.ClearDomainEvents();
        
        // Assert - Verify portfolio state
        portfolio.GetActiveInvestmentCount().Should().Be(2);
        portfolio.DomainEvents.Should().BeEmpty();
        
        // Verify portfolio calculations
        var totalValue = await valuationService.CalculateTotalValueAsync(portfolio);
        var totalCost = await valuationService.CalculateTotalCostAsync(portfolio);
        var gainLoss = await valuationService.CalculateUnrealizedGainLossAsync(portfolio);
        var percentageReturn = await valuationService.CalculatePercentageReturnAsync(portfolio);
        
        totalValue.Amount.Should().Be(2550m); // (160*10) + (190*5)
        totalCost.Amount.Should().Be(2500m); // (150*10) + (200*5)
        gainLoss.Amount.Should().Be(50m); // 2550 - 2500
        percentageReturn.Should().BeApproximately(0.02m, 0.001m); // ~2% gain
        
        // Verify diversification analysis
        var diversification = await valuationService.AnalyzeDiversificationAsync(portfolio);
        diversification.AssetTypeCount.Should().Be(1); // Only stocks
        diversification.ConcentrationRisk.Should().Be(1m); // 100% concentration for single asset type
        
        // Verify risk analysis
        var riskAnalysis = await valuationService.AnalyzeRiskAsync(portfolio);
        riskAnalysis.RiskLevel.Should().Be(RiskLevel.Moderate); // Stock investments
    }
    
    [Fact]
    public async Task PortfolioWithMultipleAssetTypes_ShouldCalculateCorrectDiversification()
    {
        // Arrange
        var portfolio = CreateDiversifiedPortfolio();
        var valuationService = new PortfolioValuationService();
        
        // Act
        var diversification = await valuationService.AnalyzeDiversificationAsync(portfolio);
        var riskAnalysis = await valuationService.AnalyzeRiskAsync(portfolio);
        
        // Assert
        diversification.AssetTypeCount.Should().Be(3); // Stock, Crypto, ETF
        diversification.DiversificationScore.Should().BeGreaterThan(50m);
        
        riskAnalysis.RiskLevel.Should().BeOneOf(RiskLevel.Moderate, RiskLevel.High); // Mixed risk
    }
    
    [Fact]
    public async Task EmptyPortfolio_ShouldHandleGracefully()
    {
        // Arrange
        var portfolio = new Portfolio(PortfolioId.New(), "Empty Portfolio", "Empty for testing", UserId.New());
        var valuationService = new PortfolioValuationService();
        
        // Act
        var totalValue = await valuationService.CalculateTotalValueAsync(portfolio);
        var totalCost = await valuationService.CalculateTotalCostAsync(portfolio);
        var gainLoss = await valuationService.CalculateUnrealizedGainLossAsync(portfolio);
        var percentageReturn = await valuationService.CalculatePercentageReturnAsync(portfolio);
        var diversification = await valuationService.AnalyzeDiversificationAsync(portfolio);
        var riskAnalysis = await valuationService.AnalyzeRiskAsync(portfolio);
        
        // Assert
        totalValue.Amount.Should().Be(0m);
        totalCost.Amount.Should().Be(0m);
        gainLoss.Amount.Should().Be(0m);
        percentageReturn.Should().Be(0m);
        diversification.AssetTypeCount.Should().Be(0);
        riskAnalysis.RiskLevel.Should().Be(RiskLevel.VeryLow);
    }
    
    [Fact]
    public void PortfolioStatusChanges_ShouldEnforceBusinessRules()
    {
        // Arrange
        var portfolio = new Portfolio(PortfolioId.New(), "Test Portfolio", "Status test", UserId.New());
        var investment = CreateInvestment("AAPL", "NASDAQ", AssetType.Stock, 150m, 10m);
        portfolio.AddInvestment(investment);
        
        // Act & Assert - Archive portfolio
        portfolio.Archive();
        portfolio.Status.Should().Be(PortfolioStatus.Archived);
        
        // Should not be able to add investments to archived portfolio
        var newInvestment = CreateInvestment("MSFT", "NASDAQ", AssetType.Stock, 200m, 5m);
        var action = () => portfolio.AddInvestment(newInvestment);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot add investments to inactive portfolio*");
        
        // Should not be able to update details
        var updateAction = () => portfolio.UpdateDetails("New Name", "New Description");
        updateAction.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot update inactive portfolio*");
    }
    
    [Fact]
    public void InvestmentStatusChanges_ShouldEnforceBusinessRules()
    {
        // Arrange
        var investment = CreateInvestment("AAPL", "NASDAQ", AssetType.Stock, 150m, 10m);
        
        // Act & Assert - Mark as sold
        investment.MarkAsSold();
        investment.Status.Should().Be(InvestmentStatus.Sold);
        
        // Should not be able to update sold investment
        var updateValueAction = () => investment.UpdateCurrentValue(new Money(160m, Currency.USD));
        updateValueAction.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot update value of inactive investment*");
        
        var updateQuantityAction = () => investment.UpdateQuantity(15m);
        updateQuantityAction.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot update quantity of inactive investment*");
        
        // Should not be able to sell again
        var sellAgainAction = () => investment.MarkAsSold();
        sellAgainAction.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot sell inactive investment*");
    }
    
    [Fact]
    public void MoneyOperations_ShouldEnforceInvariants()
    {
        // Arrange
        var usd100 = new Money(100m, Currency.USD);
        var eur100 = new Money(100m, Currency.EUR);
        
        // Act & Assert - Cannot create negative money
        var negativeAction = () => new Money(-50m, Currency.USD);
        negativeAction.Should().Throw<ArgumentException>()
            .WithMessage("Money amount cannot be negative*");
        
        // Cannot add different currencies
        var addDifferentCurrenciesAction = () => usd100.Add(eur100);
        addDifferentCurrenciesAction.Should().Throw<InvalidOperationException>()
            .WithMessage("*different currencies*");
        
        // Cannot multiply by negative factor
        var multiplyNegativeAction = () => usd100.Multiply(-2m);
        multiplyNegativeAction.Should().Throw<ArgumentException>()
            .WithMessage("Multiplication factor cannot be negative*");
        
        // Valid operations should work
        var sum = usd100.Add(new Money(50m, Currency.USD));
        sum.Amount.Should().Be(150m);
        
        var product = usd100.Multiply(2m);
        product.Amount.Should().Be(200m);
    }
    
    [Fact]
    public void SymbolValidation_ShouldEnforceInvariants()
    {
        // Act & Assert - Cannot create symbol with empty ticker
        var emptyTickerAction = () => new Symbol("", "NASDAQ", AssetType.Stock);
        emptyTickerAction.Should().Throw<ArgumentException>()
            .WithMessage("Ticker cannot be null or empty*");
        
        // Cannot create symbol with ticker too long
        var longTickerAction = () => new Symbol("VERYLONGTICKER", "NASDAQ", AssetType.Stock);
        longTickerAction.Should().Throw<ArgumentException>()
            .WithMessage("Ticker cannot exceed 10 characters*");
        
        // Cannot create symbol with empty exchange
        var emptyExchangeAction = () => new Symbol("AAPL", "", AssetType.Stock);
        emptyExchangeAction.Should().Throw<ArgumentException>()
            .WithMessage("Exchange cannot be null or empty*");
        
        // Valid symbol should work and convert to uppercase
        var symbol = new Symbol("aapl", "nasdaq", AssetType.Stock);
        symbol.Ticker.Should().Be("AAPL");
        symbol.Exchange.Should().Be("NASDAQ");
    }
    
    private Portfolio CreateDiversifiedPortfolio()
    {
        var portfolio = new Portfolio(PortfolioId.New(), "Diversified Portfolio", "Multi-asset portfolio", UserId.New());
        
        var stockInvestment = CreateInvestment("AAPL", "NASDAQ", AssetType.Stock, 150m, 10m);
        var cryptoInvestment = CreateInvestment("BTC", "BINANCE", AssetType.Crypto, 50000m, 0.1m);
        var etfInvestment = CreateInvestment("SPY", "NYSE", AssetType.ETF, 400m, 5m);
        
        portfolio.AddInvestment(stockInvestment);
        portfolio.AddInvestment(cryptoInvestment);
        portfolio.AddInvestment(etfInvestment);
        
        return portfolio;
    }
    
    private Investment CreateInvestment(string ticker, string exchange, AssetType assetType, decimal price, decimal quantity)
    {
        var investmentId = InvestmentId.New();
        var symbol = new Symbol(ticker, exchange, assetType);
        var purchasePrice = new Money(price, Currency.USD);
        var purchaseDate = DateTime.UtcNow.AddDays(-30);
        
        return new Investment(investmentId, PortfolioId.New(), symbol, purchasePrice, quantity, purchaseDate);
    }
}
