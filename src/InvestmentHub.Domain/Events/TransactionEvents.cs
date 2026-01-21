using InvestmentHub.Domain.Common;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Events;

public class BuyTransactionRecorded : DomainEvent
{
    public TransactionId TransactionId { get; }
    public PortfolioId PortfolioId { get; }
    public Symbol Symbol { get; }
    public decimal Quantity { get; }
    public Money PricePerUnit { get; }
    public Money? Fee { get; }
    public DateTime TransactionDate { get; }

    public BuyTransactionRecorded(
        TransactionId transactionId,
        PortfolioId portfolioId,
        Symbol symbol,
        decimal quantity,
        Money pricePerUnit,
        Money? fee,
        DateTime transactionDate) : base(1)
    {
        TransactionId = transactionId;
        PortfolioId = portfolioId;
        Symbol = symbol;
        Quantity = quantity;
        PricePerUnit = pricePerUnit;
        Fee = fee;
        TransactionDate = transactionDate;
    }
}

public class SellTransactionRecorded : DomainEvent
{
    public TransactionId TransactionId { get; }
    public PortfolioId PortfolioId { get; }
    public Symbol Symbol { get; }
    public decimal Quantity { get; }
    public Money SalePrice { get; }
    public Money? Fee { get; }
    public DateTime TransactionDate { get; }

    public SellTransactionRecorded(
        TransactionId transactionId,
        PortfolioId portfolioId,
        Symbol symbol,
        decimal quantity,
        Money salePrice,
        Money? fee,
        DateTime transactionDate) : base(1)
    {
        TransactionId = transactionId;
        PortfolioId = portfolioId;
        Symbol = symbol;
        Quantity = quantity;
        SalePrice = salePrice;
        Fee = fee;
        TransactionDate = transactionDate;
    }
}

public class DividendReceived : DomainEvent
{
    public TransactionId TransactionId { get; }
    public PortfolioId PortfolioId { get; }
    public Symbol Symbol { get; }
    public Money GrossAmount { get; }
    public Money? TaxWithheld { get; }
    public Money NetAmount { get; }
    public DateTime PaymentDate { get; }

    public DividendReceived(
        TransactionId transactionId,
        PortfolioId portfolioId,
        Symbol symbol,
        Money grossAmount,
        Money? taxWithheld,
        Money netAmount,
        DateTime paymentDate) : base(1)
    {
        TransactionId = transactionId;
        PortfolioId = portfolioId;
        Symbol = symbol;
        GrossAmount = grossAmount;
        TaxWithheld = taxWithheld;
        NetAmount = netAmount;
        PaymentDate = paymentDate;
    }
}

public class InterestReceived : DomainEvent
{
    public TransactionId TransactionId { get; }
    public PortfolioId PortfolioId { get; }
    public Symbol Symbol { get; }
    public Money GrossAmount { get; }
    public Money? TaxWithheld { get; }
    public Money NetAmount { get; }
    public DateTime PaymentDate { get; }

    public InterestReceived(
        TransactionId transactionId,
        PortfolioId portfolioId,
        Symbol symbol,
        Money grossAmount,
        Money? taxWithheld,
        Money netAmount,
        DateTime paymentDate) : base(1)
    {
        TransactionId = transactionId;
        PortfolioId = portfolioId;
        Symbol = symbol;
        GrossAmount = grossAmount;
        TaxWithheld = taxWithheld;
        NetAmount = netAmount;
        PaymentDate = paymentDate;
    }
}

public class TransactionUpdated : DomainEvent
{
    public TransactionId TransactionId { get; }
    public PortfolioId PortfolioId { get; }

    public TransactionUpdated(
        TransactionId transactionId,
        PortfolioId portfolioId) : base(1)
    {
        TransactionId = transactionId;
        PortfolioId = portfolioId;
    }
}

public class TransactionCancelled : DomainEvent
{
    public TransactionId TransactionId { get; }
    public PortfolioId PortfolioId { get; }

    public TransactionCancelled(
        TransactionId transactionId,
        PortfolioId portfolioId) : base(1)
    {
        TransactionId = transactionId;
        PortfolioId = portfolioId;
    }
}
