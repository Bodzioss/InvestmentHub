using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.ValueObjects;
using Xunit;

namespace InvestmentHub.Tests.Domain;

/// <summary>
/// Tests for Transaction aggregate behavior.
/// </summary>
public class TransactionAggregateTests
{
    private readonly PortfolioId _portfolioId = PortfolioId.New();
    private readonly Symbol _appleStock = new("AAPL", "NASDAQ", "Stock");
    private readonly Symbol _usBond = new("US10Y", "GOVT", "Bond");

    [Fact]
    public void RecordBuy_ShouldCreateBuyTransaction()
    {
        // Act
        var transaction = Transaction.RecordBuy(
            _portfolioId,
            _appleStock,
            quantity: 100,
            pricePerUnit: new Money(150, Currency.USD),
            transactionDate: new DateTime(2024, 1, 15),
            fee: new Money(9.99m, Currency.USD),
            notes: "Initial purchase");

        // Assert
        Assert.NotNull(transaction);
        Assert.Equal(TransactionType.BUY, transaction.Type);
        Assert.Equal(100, transaction.Quantity);
        Assert.Equal(150, transaction.PricePerUnit!.Amount);
        Assert.Equal(9.99m, transaction.Fee!.Amount);
        Assert.Equal("Initial purchase", transaction.Notes);
        Assert.Equal(TransactionStatus.Active, transaction.Status);
    }

    [Fact]
    public void RecordSell_ShouldCreateSellTransaction()
    {
        // Act
        var transaction = Transaction.RecordSell(
            _portfolioId,
            _appleStock,
            quantity: 50,
            salePrice: new Money(175, Currency.USD),
            transactionDate: new DateTime(2024, 6, 15),
            fee: new Money(9.99m, Currency.USD),
            notes: "Partial sell");

        // Assert
        Assert.Equal(TransactionType.SELL, transaction.Type);
        Assert.Equal(50, transaction.Quantity);
        Assert.Equal(175, transaction.PricePerUnit!.Amount);
    }

    [Fact]
    public void RecordDividend_WithCustomTaxRate_ShouldCalculateCorrectly()
    {
        // Arrange
        var customTaxRate = 15m; // 15% instead of default 19%

        // Act
        var transaction = Transaction.RecordDividend(
            _portfolioId,
            _appleStock,
            grossAmount: new Money(1000, Currency.USD),
            paymentDate: DateTime.UtcNow,
            taxRate: customTaxRate,
            notes: "Q4 dividend");

        // Assert
        Assert.Equal(TransactionType.DIVIDEND, transaction.Type);
        Assert.Equal(1000, transaction.GrossAmount!.Amount);
        Assert.Equal(15, transaction.TaxRate);
        Assert.Equal(150, transaction.TaxWithheld!.Amount); // 15% of 1000
        Assert.Equal(850, transaction.NetAmount!.Amount); // 1000 - 150
    }

    [Fact]
    public void RecordInterest_ForBond_ShouldWork()
    {
        // Act
        var transaction = Transaction.RecordInterest(
            _portfolioId,
            _usBond,
            grossAmount: new Money(250, Currency.USD),
            paymentDate: DateTime.UtcNow,
            taxRate: 19m,
            notes: "Semi-annual coupon");

        // Assert
        Assert.Equal(TransactionType.INTEREST, transaction.Type);
        Assert.Equal(250, transaction.GrossAmount!.Amount);
        Assert.Equal(47.5m, transaction.TaxWithheld!.Amount);
        Assert.Equal(202.5m, transaction.NetAmount!.Amount);
    }

    [Fact]
    public void BuyWithMaturityDate_ShouldStoreBondMaturity()
    {
        // Act
        var maturityDate = new DateTime(2034, 1, 15);
        var transaction = Transaction.RecordBuy(
            _portfolioId,
            _usBond,
            quantity: 10,
            pricePerUnit: new Money(1000, Currency.USD),
            transactionDate: DateTime.UtcNow,
            fee: null,
            maturityDate: maturityDate,
            notes: "10-year bond");

        // Assert
        Assert.Equal(maturityDate, transaction.MaturityDate);
    }

    [Fact]
    public void Cancel_ActiveTransaction_ShouldChangeStatusToCancelled()
    {
        // Arrange
        var transaction = Transaction.RecordBuy(
            _portfolioId,
            _appleStock,
            10,
            new Money(100, Currency.USD),
            DateTime.UtcNow);

        Assert.Equal(TransactionStatus.Active, transaction.Status);

        // Act
        transaction.Cancel();

        // Assert
        Assert.Equal(TransactionStatus.Cancelled, transaction.Status);
    }

    [Fact]
    public void Update_BuyTransaction_ShouldModifyQuantityAndPrice()
    {
        // Arrange
        var transaction = Transaction.RecordBuy(
            _portfolioId,
            _appleStock,
            quantity: 10,
            pricePerUnit: new Money(100, Currency.USD),
            transactionDate: DateTime.UtcNow);

        // Act
        transaction.Update(
            quantity: 15,
            pricePerUnit: new Money(105, Currency.USD),
            fee: new Money(12, Currency.USD),
            grossAmount: null,
            taxRate: null,
            transactionDate: null,
            notes: "Corrected quantity");

        // Assert
        Assert.Equal(15, transaction.Quantity);
        Assert.Equal(105, transaction.PricePerUnit!.Amount);
        Assert.Equal(12, transaction.Fee!.Amount);
        Assert.Equal("Corrected quantity", transaction.Notes);
    }

    [Fact]
    public void Update_DividendTransaction_ShouldRecalculateTax()
    {
        // Arrange
        var transaction = Transaction.RecordDividend(
            _portfolioId,
            _appleStock,
            grossAmount: new Money(1000, Currency.USD),
            paymentDate: DateTime.UtcNow,
            taxRate: 19m);

        // Act - change gross amount and tax rate
        transaction.Update(
            quantity: null,
            pricePerUnit: null,
            fee: null,
            grossAmount: new Money(1200, Currency.USD),
            taxRate: 15m,
            transactionDate: null,
            notes: null);

        // Assert
        Assert.Equal(1200, transaction.GrossAmount!.Amount);
        Assert.Equal(15, transaction.TaxRate);
        Assert.Equal(180, transaction.TaxWithheld!.Amount); // 15% of 1200
        Assert.Equal(1020, transaction.NetAmount!.Amount); // 1200 - 180
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void RecordBuy_NegativeOrZeroQuantity_ShouldThrow(decimal invalidQuantity)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Transaction.RecordBuy(
                _portfolioId,
                _appleStock,
                quantity: invalidQuantity,
                pricePerUnit: new Money(100, Currency.USD),
                transactionDate: DateTime.UtcNow));
    }

    [Fact]
    public void RecordDividend_NegativeGrossAmount_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Transaction.RecordDividend(
                _portfolioId,
                _appleStock,
                grossAmount: new Money(-100, Currency.USD),
                paymentDate: DateTime.UtcNow,
                taxRate: 19m));
    }
}
