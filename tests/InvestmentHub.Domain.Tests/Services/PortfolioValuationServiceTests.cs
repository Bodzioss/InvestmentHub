using FluentAssertions;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Services;
using InvestmentHub.Domain.ValueObjects;
using Xunit;

namespace InvestmentHub.Domain.Tests.Services;

/// <summary>
/// Testy dla PortfolioValuationService sprawdzające:
/// - Obliczenia wartości portfela (CalculateTotalValueAsync, CalculateTotalCostAsync)
/// - Obliczenia zysków i strat (CalculateUnrealizedGainLossAsync, CalculatePercentageReturnAsync)
/// - Analizę dywersyfikacji (AnalyzeDiversificationAsync)
/// - Identyfikację najlepszych i najgorszych inwestycji (GetTopPerformersAsync, GetWorstPerformersAsync)
/// - Analizę ryzyka (AnalyzeRiskAsync)
/// - Obsługę pustych portfeli
/// - Walidację parametrów wejściowych
/// </summary>
public class PortfolioValuationServiceTests
{
    private readonly IPortfolioValuationService _valuationService;
    private readonly PortfolioId _portfolioId;
    private readonly UserId _ownerId;
    
    public PortfolioValuationServiceTests()
    {
        _valuationService = new PortfolioValuationService();
        _portfolioId = PortfolioId.New();
        _ownerId = UserId.New();
    }
    
    [Fact]
    public async Task CalculateTotalValueAsync_WithEmptyPortfolio_ShouldReturnZero()
    {
        // Arrange
        var portfolio = CreateEmptyPortfolio();
        
        // Act
        var totalValue = await _valuationService.CalculateTotalValueAsync(portfolio);
        
        // Assert
        totalValue.Amount.Should().Be(0m);
        totalValue.Currency.Should().Be(Currency.USD);
    }
    
    [Fact]
    public async Task CalculateTotalValueAsync_WithInvestments_ShouldReturnSumOfCurrentValues()
    {
        // Arrange
        var portfolio = CreatePortfolioWithInvestments();
        
        // Act
        var totalValue = await _valuationService.CalculateTotalValueAsync(portfolio);
        
        // Assert
        totalValue.Amount.Should().Be(3200m); // 1500 + 1700
        totalValue.Currency.Should().Be(Currency.USD);
    }
    
    [Fact]
    public async Task CalculateTotalCostAsync_WithEmptyPortfolio_ShouldReturnZero()
    {
        // Arrange
        var portfolio = CreateEmptyPortfolio();
        
        // Act
        var totalCost = await _valuationService.CalculateTotalCostAsync(portfolio);
        
        // Assert
        totalCost.Amount.Should().Be(0m);
        totalCost.Currency.Should().Be(Currency.USD);
    }
    
    [Fact]
    public async Task CalculateTotalCostAsync_WithInvestments_ShouldReturnSumOfTotalCosts()
    {
        // Arrange
        var portfolio = CreatePortfolioWithInvestments();
        
        // Act
        var totalCost = await _valuationService.CalculateTotalCostAsync(portfolio);
        
        // Assert
        totalCost.Amount.Should().Be(3000m); // 1500 + 1500
        totalCost.Currency.Should().Be(Currency.USD);
    }
    
    [Fact]
    public async Task CalculateUnrealizedGainLossAsync_WithEmptyPortfolio_ShouldReturnZero()
    {
        // Arrange
        var portfolio = CreateEmptyPortfolio();
        
        // Act
        var gainLoss = await _valuationService.CalculateUnrealizedGainLossAsync(portfolio);
        
        // Assert
        gainLoss.Amount.Should().Be(0m);
        gainLoss.Currency.Should().Be(Currency.USD);
    }
    
    [Fact]
    public async Task CalculateUnrealizedGainLossAsync_WithProfitableInvestments_ShouldReturnPositiveGain()
    {
        // Arrange
        var portfolio = CreatePortfolioWithInvestments();
        
        // Act
        var gainLoss = await _valuationService.CalculateUnrealizedGainLossAsync(portfolio);
        
        // Assert
        gainLoss.Amount.Should().Be(200m); // 3200 - 3000
        gainLoss.Currency.Should().Be(Currency.USD);
    }
    
    [Fact]
    public async Task CalculatePercentageReturnAsync_WithEmptyPortfolio_ShouldReturnZero()
    {
        // Arrange
        var portfolio = CreateEmptyPortfolio();
        
        // Act
        var percentageReturn = await _valuationService.CalculatePercentageReturnAsync(portfolio);
        
        // Assert
        percentageReturn.Should().Be(0m);
    }
    
    [Fact]
    public async Task CalculatePercentageReturnAsync_WithProfitableInvestments_ShouldReturnPositivePercentage()
    {
        // Arrange
        var portfolio = CreatePortfolioWithInvestments();
        
        // Act
        var percentageReturn = await _valuationService.CalculatePercentageReturnAsync(portfolio);
        
        // Assert
        percentageReturn.Should().BeApproximately(0.0667m, 0.001m); // ~6.67% gain
    }
    
    [Fact]
    public async Task AnalyzeDiversificationAsync_WithEmptyPortfolio_ShouldReturnZeroMetrics()
    {
        // Arrange
        var portfolio = CreateEmptyPortfolio();
        
        // Act
        var diversification = await _valuationService.AnalyzeDiversificationAsync(portfolio);
        
        // Assert
        diversification.AssetTypeCount.Should().Be(0);
        diversification.SectorCount.Should().Be(0);
        diversification.ConcentrationRisk.Should().Be(0m);
        diversification.DiversificationScore.Should().Be(0m);
    }
    
    [Fact]
    public async Task AnalyzeDiversificationAsync_WithSingleAssetType_ShouldReturnLowDiversification()
    {
        // Arrange
        var portfolio = CreatePortfolioWithSingleAssetType();
        
        // Act
        var diversification = await _valuationService.AnalyzeDiversificationAsync(portfolio);
        
        // Assert
        diversification.AssetTypeCount.Should().Be(1);
        diversification.ConcentrationRisk.Should().Be(1m); // 100% concentration
        diversification.DiversificationScore.Should().BeLessThan(50m);
    }
    
    [Fact]
    public async Task AnalyzeDiversificationAsync_WithMultipleAssetTypes_ShouldReturnHigherDiversification()
    {
        // Arrange
        var portfolio = CreatePortfolioWithMultipleAssetTypes();
        
        // Act
        var diversification = await _valuationService.AnalyzeDiversificationAsync(portfolio);
        
        // Assert
        diversification.AssetTypeCount.Should().Be(3); // Stock, Crypto, ETF
        diversification.ConcentrationRisk.Should().BeLessThan(1m);
        diversification.DiversificationScore.Should().BeGreaterThan(50m);
    }
    
    [Fact]
    public async Task GetTopPerformersAsync_WithEmptyPortfolio_ShouldReturnEmptyCollection()
    {
        // Arrange
        var portfolio = CreateEmptyPortfolio();
        
        // Act
        var topPerformers = await _valuationService.GetTopPerformersAsync(portfolio, 5);
        
        // Assert
        topPerformers.Should().BeEmpty();
    }
    
    [Fact]
    public async Task GetTopPerformersAsync_WithInvestments_ShouldReturnTopPerformers()
    {
        // Arrange
        var portfolio = CreatePortfolioWithInvestments();
        
        // Act
        var topPerformers = await _valuationService.GetTopPerformersAsync(portfolio, 2);
        
        // Assert
        topPerformers.Should().HaveCount(2);
        var performers = topPerformers.ToList();
        performers[0].PercentageReturn.Should().BeGreaterThanOrEqualTo(performers[1].PercentageReturn);
    }
    
    [Fact]
    public async Task GetWorstPerformersAsync_WithEmptyPortfolio_ShouldReturnEmptyCollection()
    {
        // Arrange
        var portfolio = CreateEmptyPortfolio();
        
        // Act
        var worstPerformers = await _valuationService.GetWorstPerformersAsync(portfolio, 5);
        
        // Assert
        worstPerformers.Should().BeEmpty();
    }
    
    [Fact]
    public async Task GetWorstPerformersAsync_WithInvestments_ShouldReturnWorstPerformers()
    {
        // Arrange
        var portfolio = CreatePortfolioWithInvestments();
        
        // Act
        var worstPerformers = await _valuationService.GetWorstPerformersAsync(portfolio, 2);
        
        // Assert
        worstPerformers.Should().HaveCount(2);
        var performers = worstPerformers.ToList();
        performers[0].PercentageReturn.Should().BeLessThanOrEqualTo(performers[1].PercentageReturn);
    }
    
    [Fact]
    public async Task AnalyzeRiskAsync_WithEmptyPortfolio_ShouldReturnVeryLowRisk()
    {
        // Arrange
        var portfolio = CreateEmptyPortfolio();
        
        // Act
        var riskAnalysis = await _valuationService.AnalyzeRiskAsync(portfolio);
        
        // Assert
        riskAnalysis.RiskLevel.Should().Be(RiskLevel.VeryLow);
        riskAnalysis.RiskScore.Should().Be(0m);
    }
    
    [Fact]
    public async Task AnalyzeRiskAsync_WithStockInvestments_ShouldReturnModerateRisk()
    {
        // Arrange
        var portfolio = CreatePortfolioWithSingleAssetType();
        
        // Act
        var riskAnalysis = await _valuationService.AnalyzeRiskAsync(portfolio);
        
        // Assert
        riskAnalysis.RiskLevel.Should().Be(RiskLevel.Moderate);
        riskAnalysis.RiskScore.Should().Be(30m); // Stock risk weight
    }
    
    [Fact]
    public async Task AnalyzeRiskAsync_WithCryptoInvestments_ShouldReturnHighRisk()
    {
        // Arrange
        var portfolio = CreatePortfolioWithCryptoInvestments();
        
        // Act
        var riskAnalysis = await _valuationService.AnalyzeRiskAsync(portfolio);
        
        // Assert
        riskAnalysis.RiskLevel.Should().Be(RiskLevel.High);
        riskAnalysis.RiskScore.Should().Be(80m); // Crypto risk weight
    }
    
    [Fact]
    public async Task CalculateTotalValueAsync_WithNullPortfolio_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = async () => await _valuationService.CalculateTotalValueAsync(null!);
        
        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("portfolio");
    }
    
    [Fact]
    public async Task CalculateTotalCostAsync_WithNullPortfolio_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = async () => await _valuationService.CalculateTotalCostAsync(null!);
        
        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("portfolio");
    }
    
    [Fact]
    public async Task CalculateUnrealizedGainLossAsync_WithNullPortfolio_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = async () => await _valuationService.CalculateUnrealizedGainLossAsync(null!);
        
        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("portfolio");
    }
    
    [Fact]
    public async Task CalculatePercentageReturnAsync_WithNullPortfolio_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = async () => await _valuationService.CalculatePercentageReturnAsync(null!);
        
        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("portfolio");
    }
    
    [Fact]
    public async Task AnalyzeDiversificationAsync_WithNullPortfolio_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = async () => await _valuationService.AnalyzeDiversificationAsync(null!);
        
        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("portfolio");
    }
    
    [Fact]
    public async Task GetTopPerformersAsync_WithNullPortfolio_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = async () => await _valuationService.GetTopPerformersAsync(null!, 5);
        
        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("portfolio");
    }
    
    [Fact]
    public async Task GetWorstPerformersAsync_WithNullPortfolio_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = async () => await _valuationService.GetWorstPerformersAsync(null!, 5);
        
        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("portfolio");
    }
    
    [Fact]
    public async Task AnalyzeRiskAsync_WithNullPortfolio_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var action = async () => await _valuationService.AnalyzeRiskAsync(null!);
        
        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("portfolio");
    }
    
    private Portfolio CreateEmptyPortfolio()
    {
        return new Portfolio(_portfolioId, "Empty Portfolio", "Empty portfolio for testing", _ownerId);
    }
    
    private Portfolio CreatePortfolioWithInvestments()
    {
        var portfolio = new Portfolio(_portfolioId, "Test Portfolio", "Portfolio with investments", _ownerId);
        
        // Add profitable investment
        var investment1 = CreateInvestment("AAPL", "NASDAQ", AssetType.Stock, 150m, 10m);
        investment1.UpdateCurrentValue(new Money(150m, Currency.USD)); // Break even
        portfolio.AddInvestment(investment1);
        
        // Add profitable investment
        var investment2 = CreateInvestment("MSFT", "NASDAQ", AssetType.Stock, 150m, 10m);
        investment2.UpdateCurrentValue(new Money(170m, Currency.USD)); // 13.33% gain
        portfolio.AddInvestment(investment2);
        
        return portfolio;
    }
    
    private Portfolio CreatePortfolioWithSingleAssetType()
    {
        var portfolio = new Portfolio(_portfolioId, "Single Asset Portfolio", "Portfolio with single asset type", _ownerId);
        
        var investment1 = CreateInvestment("AAPL", "NASDAQ", AssetType.Stock, 100m, 10m);
        var investment2 = CreateInvestment("MSFT", "NASDAQ", AssetType.Stock, 100m, 10m);
        
        portfolio.AddInvestment(investment1);
        portfolio.AddInvestment(investment2);
        
        return portfolio;
    }
    
    private Portfolio CreatePortfolioWithMultipleAssetTypes()
    {
        var portfolio = new Portfolio(_portfolioId, "Diversified Portfolio", "Portfolio with multiple asset types", _ownerId);
        
        var stockInvestment = CreateInvestment("AAPL", "NASDAQ", AssetType.Stock, 100m, 10m);
        var cryptoInvestment = CreateInvestment("BTC", "BINANCE", AssetType.Crypto, 100m, 1m);
        var etfInvestment = CreateInvestment("SPY", "NYSE", AssetType.ETF, 100m, 5m);
        
        portfolio.AddInvestment(stockInvestment);
        portfolio.AddInvestment(cryptoInvestment);
        portfolio.AddInvestment(etfInvestment);
        
        return portfolio;
    }
    
    private Portfolio CreatePortfolioWithCryptoInvestments()
    {
        var portfolio = new Portfolio(_portfolioId, "Crypto Portfolio", "Portfolio with crypto investments", _ownerId);
        
        var cryptoInvestment = CreateInvestment("BTC", "BINANCE", AssetType.Crypto, 100m, 1m);
        portfolio.AddInvestment(cryptoInvestment);
        
        return portfolio;
    }
    
    private static Investment CreateInvestment(string ticker, string exchange, AssetType assetType, decimal price, decimal quantity)
    {
        var investmentId = InvestmentId.New();
        var symbol = new Symbol(ticker, exchange, assetType);
        var purchasePrice = new Money(price, Currency.USD);
        var purchaseDate = DateTime.UtcNow.AddDays(-30);
        
        return new Investment(investmentId, PortfolioId.New(), symbol, purchasePrice, quantity, purchaseDate);
    }
}
