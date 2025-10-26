using FluentAssertions;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.ValueObjects;
using Xunit;

namespace InvestmentHub.Domain.Tests.ValueObjects;

/// <summary>
/// Testy dla Symbol value object sprawdzające:
/// - Invarianty (ticker nie może być pusty, max 10 znaków)
/// - Automatyczna konwersja na wielkie litery
/// - Walidację exchange (nie może być pusty)
/// - Równość i porównania
/// - Metody fabryczne (Stock, Crypto, ETF)
/// - Metody pomocnicze (GetFullSymbol, ToString)
/// </summary>
public class SymbolTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateSymbol()
    {
        // Arrange & Act
        var symbol = new Symbol("AAPL", "NASDAQ", AssetType.Stock);
        
        // Assert
        symbol.Ticker.Should().Be("AAPL");
        symbol.Exchange.Should().Be("NASDAQ");
        symbol.AssetType.Should().Be(AssetType.Stock);
    }
    
    [Fact]
    public void Constructor_WithLowercaseTicker_ShouldConvertToUppercase()
    {
        // Arrange & Act
        var symbol = new Symbol("aapl", "nasdaq", AssetType.Stock);
        
        // Assert
        symbol.Ticker.Should().Be("AAPL");
        symbol.Exchange.Should().Be("NASDAQ");
    }
    
    [Fact]
    public void Constructor_WithMixedCaseExchange_ShouldConvertToUppercase()
    {
        // Arrange & Act
        var symbol = new Symbol("MSFT", "NySe", AssetType.Stock);
        
        // Assert
        symbol.Ticker.Should().Be("MSFT");
        symbol.Exchange.Should().Be("NYSE");
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidTicker_ShouldThrowArgumentException(string ticker)
    {
        // Arrange & Act
        var action = () => new Symbol(ticker, "NASDAQ", AssetType.Stock);
        
        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Ticker cannot be null or empty*")
            .And.ParamName.Should().Be("ticker");
    }
    
    [Fact]
    public void Constructor_WithTickerTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var longTicker = new string('A', 11); // 11 characters
        
        // Act
        var action = () => new Symbol(longTicker, "NASDAQ", AssetType.Stock);
        
        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Ticker cannot exceed 10 characters*")
            .And.ParamName.Should().Be("ticker");
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidExchange_ShouldThrowArgumentException(string exchange)
    {
        // Arrange & Act
        var action = () => new Symbol("AAPL", exchange, AssetType.Stock);
        
        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Exchange cannot be null or empty*")
            .And.ParamName.Should().Be("exchange");
    }
    
    [Fact]
    public void Stock_ShouldCreateStockSymbol()
    {
        // Arrange & Act
        var symbol = Symbol.Stock("AAPL", "NASDAQ");
        
        // Assert
        symbol.Ticker.Should().Be("AAPL");
        symbol.Exchange.Should().Be("NASDAQ");
        symbol.AssetType.Should().Be(AssetType.Stock);
    }
    
    [Fact]
    public void Crypto_ShouldCreateCryptoSymbol()
    {
        // Arrange & Act
        var symbol = Symbol.Crypto("BTC", "BINANCE");
        
        // Assert
        symbol.Ticker.Should().Be("BTC");
        symbol.Exchange.Should().Be("BINANCE");
        symbol.AssetType.Should().Be(AssetType.Crypto);
    }
    
    [Fact]
    public void ETF_ShouldCreateETFSymbol()
    {
        // Arrange & Act
        var symbol = Symbol.ETF("SPY", "NYSE");
        
        // Assert
        symbol.Ticker.Should().Be("SPY");
        symbol.Exchange.Should().Be("NYSE");
        symbol.AssetType.Should().Be(AssetType.ETF);
    }
    
    [Fact]
    public void GetFullSymbol_ShouldReturnTickerAndExchange()
    {
        // Arrange
        var symbol = new Symbol("AAPL", "NASDAQ", AssetType.Stock);
        
        // Act
        var fullSymbol = symbol.GetFullSymbol();
        
        // Assert
        fullSymbol.Should().Be("AAPL.NASDAQ");
    }
    
    [Fact]
    public void Equals_WithSameProperties_ShouldReturnTrue()
    {
        // Arrange
        var symbol1 = new Symbol("AAPL", "NASDAQ", AssetType.Stock);
        var symbol2 = new Symbol("AAPL", "NASDAQ", AssetType.Stock);
        
        // Act & Assert
        symbol1.Should().Be(symbol2);
        symbol1.Equals(symbol2).Should().BeTrue();
        (symbol1 == symbol2).Should().BeTrue();
        (symbol1 != symbol2).Should().BeFalse();
    }
    
    [Fact]
    public void Equals_WithDifferentTicker_ShouldReturnFalse()
    {
        // Arrange
        var symbol1 = new Symbol("AAPL", "NASDAQ", AssetType.Stock);
        var symbol2 = new Symbol("MSFT", "NASDAQ", AssetType.Stock);
        
        // Act & Assert
        symbol1.Should().NotBe(symbol2);
        symbol1.Equals(symbol2).Should().BeFalse();
        (symbol1 == symbol2).Should().BeFalse();
        (symbol1 != symbol2).Should().BeTrue();
    }
    
    [Fact]
    public void Equals_WithDifferentExchange_ShouldReturnFalse()
    {
        // Arrange
        var symbol1 = new Symbol("AAPL", "NASDAQ", AssetType.Stock);
        var symbol2 = new Symbol("AAPL", "NYSE", AssetType.Stock);
        
        // Act & Assert
        symbol1.Should().NotBe(symbol2);
        symbol1.Equals(symbol2).Should().BeFalse();
    }
    
    [Fact]
    public void Equals_WithDifferentAssetType_ShouldReturnFalse()
    {
        // Arrange
        var symbol1 = new Symbol("AAPL", "NASDAQ", AssetType.Stock);
        var symbol2 = new Symbol("AAPL", "NASDAQ", AssetType.ETF);
        
        // Act & Assert
        symbol1.Should().NotBe(symbol2);
        symbol1.Equals(symbol2).Should().BeFalse();
    }
    
    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var symbol = new Symbol("AAPL", "NASDAQ", AssetType.Stock);
        
        // Act & Assert
        symbol.Equals(null).Should().BeFalse();
        (symbol == null).Should().BeFalse();
        (symbol != null).Should().BeTrue();
    }
    
    [Fact]
    public void GetHashCode_WithSameProperties_ShouldReturnSameHashCode()
    {
        // Arrange
        var symbol1 = new Symbol("AAPL", "NASDAQ", AssetType.Stock);
        var symbol2 = new Symbol("AAPL", "NASDAQ", AssetType.Stock);
        
        // Act & Assert
        symbol1.GetHashCode().Should().Be(symbol2.GetHashCode());
    }
    
    [Fact]
    public void GetHashCode_WithDifferentProperties_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var symbol1 = new Symbol("AAPL", "NASDAQ", AssetType.Stock);
        var symbol2 = new Symbol("MSFT", "NASDAQ", AssetType.Stock);
        
        // Act & Assert
        symbol1.GetHashCode().Should().NotBe(symbol2.GetHashCode());
    }
    
    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var symbol = new Symbol("AAPL", "NASDAQ", AssetType.Stock);
        
        // Act
        var result = symbol.ToString();
        
        // Assert
        result.Should().Contain("AAPL");
        result.Should().Contain("NASDAQ");
        result.Should().Contain("Stock");
    }
    
    [Theory]
    [InlineData("A", "EXCHANGE", AssetType.Stock)] // 1 character
    [InlineData("ABCDEFGHIJ", "EXCHANGE", AssetType.Stock)] // 10 characters (max)
    public void Constructor_WithValidTickerLength_ShouldCreateSymbol(string ticker, string exchange, AssetType assetType)
    {
        // Arrange & Act
        var symbol = new Symbol(ticker, exchange, assetType);
        
        // Assert
        symbol.Ticker.Should().Be(ticker.ToUpperInvariant());
        symbol.Exchange.Should().Be(exchange.ToUpperInvariant());
        symbol.AssetType.Should().Be(assetType);
    }
}
