using InvestmentHub.Domain.ValueObjects;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace InvestmentHub.Domain.Commands;

/// <summary>
/// Command to record a DIVIDEND transaction for a portfolio.
/// </summary>
public record RecordDividendTransactionCommand : IRequest<RecordIncomeTransactionResult>
{
    public PortfolioId PortfolioId { get; init; }
    public Symbol Symbol { get; init; }

    [Required(ErrorMessage = "Gross amount is required")]
    public Money GrossAmount { get; init; }

    [Required(ErrorMessage = "Payment date is required")]
    public DateTime PaymentDate { get; init; }

    public decimal? TaxRate { get; init; }
    public string? Notes { get; init; }
    public TransactionId TransactionId { get; init; }

    public RecordDividendTransactionCommand(
        PortfolioId portfolioId,
        Symbol symbol,
        Money grossAmount,
        DateTime paymentDate,
        decimal? taxRate = null,
        string? notes = null)
    {
        PortfolioId = portfolioId;
        Symbol = symbol;
        GrossAmount = grossAmount;
        PaymentDate = paymentDate;
        TaxRate = taxRate;
        Notes = notes;
        TransactionId = TransactionId.New();
    }
}

/// <summary>
/// Command to record an INTEREST transaction for a portfolio (bonds).
/// </summary>
public record RecordInterestTransactionCommand : IRequest<RecordIncomeTransactionResult>
{
    public PortfolioId PortfolioId { get; init; }
    public Symbol Symbol { get; init; }

    [Required(ErrorMessage = "Gross amount is required")]
    public Money GrossAmount { get; init; }

    [Required(ErrorMessage = "Payment date is required")]
    public DateTime PaymentDate { get; init; }

    public decimal? TaxRate { get; init; }
    public string? Notes { get; init; }
    public TransactionId TransactionId { get; init; }

    public RecordInterestTransactionCommand(
        PortfolioId portfolioId,
        Symbol symbol,
        Money grossAmount,
        DateTime paymentDate,
        decimal? taxRate = null,
        string? notes = null)
    {
        PortfolioId = portfolioId;
        Symbol = symbol;
        GrossAmount = grossAmount;
        PaymentDate = paymentDate;
        TaxRate = taxRate;
        Notes = notes;
        TransactionId = TransactionId.New();
    }
}

/// <summary>
/// Result for dividend and interest transaction commands.
/// </summary>
public record RecordIncomeTransactionResult
{
    public TransactionId TransactionId { get; init; } = null!;
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static RecordIncomeTransactionResult Success(TransactionId transactionId) =>
        new() { TransactionId = transactionId, IsSuccess = true };

    public static RecordIncomeTransactionResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
