using InvestmentHub.Domain.Common;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Events;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Aggregates;

/// <summary>
/// Transaction aggregate representing a buy, sell, dividend, or interest transaction
/// </summary>
public class Transaction : AggregateRoot
{
    public TransactionId Id { get; private set; } = null!;
    public PortfolioId PortfolioId { get; private set; } = null!;
    public TransactionType Type { get; private set; }
    public Symbol Symbol { get; private set; } = null!;

    // For BUY/SELL transactions
    public decimal? Quantity { get; private set; }
    public Money? PricePerUnit { get; private set; }
    public Money? Fee { get; private set; } // Transaction fee/commission

    // For DIVIDEND/INTEREST transactions
    public Money? GrossAmount { get; private set; } // Before tax
    public decimal? TaxRate { get; private set; } // Tax rate percentage (e.g., 19.00)
    public Money? TaxWithheld { get; private set; } // Tax amount
    public Money? NetAmount { get; private set; } // After tax amount received

    // For BOND instruments
    public DateTime? MaturityDate { get; private set; }

    public DateTime TransactionDate { get; private set; }
    public TransactionStatus Status { get; private set; }
    public string? Notes { get; private set; }

    // EF Core constructor
    private Transaction() { }

    /// <summary>
    /// Converts DateTime to UTC for PostgreSQL compatibility
    /// </summary>
    private static DateTime ToUtc(DateTime dateTime) =>
        dateTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
            : dateTime.ToUniversalTime();

    /// <summary>
    /// Records a BUY transaction
    /// </summary>
    public static Transaction RecordBuy(
        PortfolioId portfolioId,
        Symbol symbol,
        decimal quantity,
        Money pricePerUnit,
        DateTime transactionDate,
        Money? fee = null,
        DateTime? maturityDate = null,
        string? notes = null)
    {
        var transaction = new Transaction
        {
            Id = TransactionId.New(),
            PortfolioId = portfolioId,
            Type = TransactionType.BUY,
            Symbol = symbol,
            Quantity = quantity,
            PricePerUnit = pricePerUnit,
            Fee = fee,
            MaturityDate = maturityDate.HasValue ? ToUtc(maturityDate.Value) : null,
            TransactionDate = ToUtc(transactionDate),
            Status = TransactionStatus.Active,
            Notes = notes
        };

        transaction.AddDomainEvent(new BuyTransactionRecorded(
            transaction.Id,
            portfolioId,
            symbol,
            quantity,
            pricePerUnit,
            fee,
            transactionDate
        ));

        return transaction;
    }

    /// <summary>
    /// Records a SELL transaction
    /// </summary>
    public static Transaction RecordSell(
        PortfolioId portfolioId,
        Symbol symbol,
        decimal quantity,
        Money salePrice,
        DateTime transactionDate,
        Money? fee = null,
        string? notes = null)
    {
        var transaction = new Transaction
        {
            Id = TransactionId.New(),
            PortfolioId = portfolioId,
            Type = TransactionType.SELL,
            Symbol = symbol,
            Quantity = quantity,
            PricePerUnit = salePrice,
            Fee = fee,
            TransactionDate = ToUtc(transactionDate),
            Status = TransactionStatus.Active,
            Notes = notes
        };

        transaction.AddDomainEvent(new SellTransactionRecorded(
            transaction.Id,
            portfolioId,
            symbol,
            quantity,
            salePrice,
            fee,
            transactionDate
        ));

        return transaction;
    }

    /// <summary>
    /// Records a DIVIDEND transaction
    /// </summary>
    public static Transaction RecordDividend(
        PortfolioId portfolioId,
        Symbol symbol,
        Money grossAmount,
        DateTime paymentDate,
        decimal? taxRate = null,
        string? notes = null)
    {
        var taxWithheld = taxRate.HasValue
            ? new Money(grossAmount.Amount * (taxRate.Value / 100), grossAmount.Currency)
            : null;

        var netAmount = taxWithheld != null
            ? new Money(grossAmount.Amount - taxWithheld.Amount, grossAmount.Currency)
            : grossAmount;

        var transaction = new Transaction
        {
            Id = TransactionId.New(),
            PortfolioId = portfolioId,
            Type = TransactionType.DIVIDEND,
            Symbol = symbol,
            GrossAmount = grossAmount,
            TaxRate = taxRate,
            TaxWithheld = taxWithheld,
            NetAmount = netAmount,
            TransactionDate = ToUtc(paymentDate),
            Status = TransactionStatus.Active,
            Notes = notes
        };

        transaction.AddDomainEvent(new DividendReceived(
            transaction.Id,
            portfolioId,
            symbol,
            grossAmount,
            taxWithheld,
            netAmount,
            paymentDate
        ));

        return transaction;
    }

    /// <summary>
    /// Records an INTEREST transaction (for bonds)
    /// </summary>
    public static Transaction RecordInterest(
        PortfolioId portfolioId,
        Symbol symbol,
        Money grossAmount,
        DateTime paymentDate,
        decimal? taxRate = null,
        string? notes = null)
    {
        var taxWithheld = taxRate.HasValue
            ? new Money(grossAmount.Amount * (taxRate.Value / 100), grossAmount.Currency)
            : null;

        var netAmount = taxWithheld != null
            ? new Money(grossAmount.Amount - taxWithheld.Amount, grossAmount.Currency)
            : grossAmount;

        var transaction = new Transaction
        {
            Id = TransactionId.New(),
            PortfolioId = portfolioId,
            Type = TransactionType.INTEREST,
            Symbol = symbol,
            GrossAmount = grossAmount,
            TaxRate = taxRate,
            TaxWithheld = taxWithheld,
            NetAmount = netAmount,
            TransactionDate = ToUtc(paymentDate),
            Status = TransactionStatus.Active,
            Notes = notes
        };

        transaction.AddDomainEvent(new InterestReceived(
            transaction.Id,
            portfolioId,
            symbol,
            grossAmount,
            taxWithheld,
            netAmount,
            paymentDate
        ));

        return transaction;
    }

    /// <summary>
    /// Updates transaction details
    /// </summary>
    public void Update(
        decimal? quantity = null,
        Money? pricePerUnit = null,
        Money? fee = null,
        Money? grossAmount = null,
        decimal? taxRate = null,
        DateTime? transactionDate = null,
        string? notes = null)
    {
        if (Status == TransactionStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot update a cancelled transaction");
        }

        // Update based on transaction type
        if (Type == TransactionType.BUY || Type == TransactionType.SELL)
        {
            if (quantity.HasValue) Quantity = quantity.Value;
            if (pricePerUnit != null) PricePerUnit = pricePerUnit;
            if (fee != null) Fee = fee;
        }
        else if (Type == TransactionType.DIVIDEND || Type == TransactionType.INTEREST)
        {
            if (grossAmount != null)
            {
                GrossAmount = grossAmount;

                // Recalculate tax if rate provided
                if (taxRate.HasValue)
                {
                    TaxRate = taxRate;
                    TaxWithheld = new Money(grossAmount.Amount * (taxRate.Value / 100), grossAmount.Currency);
                    NetAmount = new Money(grossAmount.Amount - TaxWithheld.Amount, grossAmount.Currency);
                }
                else if (!TaxRate.HasValue)
                {
                    NetAmount = grossAmount;
                }
            }
        }

        if (transactionDate.HasValue) TransactionDate = transactionDate.Value;
        if (notes != null) Notes = notes;

        AddDomainEvent(new TransactionUpdated(Id, PortfolioId));
    }

    /// <summary>
    /// Cancels the transaction
    /// </summary>
    public void Cancel()
    {
        if (Status == TransactionStatus.Cancelled)
        {
            throw new InvalidOperationException("Transaction is already cancelled");
        }

        Status = TransactionStatus.Cancelled;
        AddDomainEvent(new TransactionCancelled(Id, PortfolioId));
    }
}
