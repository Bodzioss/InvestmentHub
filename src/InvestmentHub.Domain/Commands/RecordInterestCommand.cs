using InvestmentHub.Domain.ValueObjects;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace InvestmentHub.Domain.Commands;

/// <summary>
/// Command to record an INTEREST payment (for bonds).
/// </summary>
public record RecordInterestCommand : IRequest<RecordInterestResult>
{
    public PortfolioId PortfolioId { get; init; }
    public Symbol Symbol { get; init; } // Bond symbol

    [Range(0.01, double.MaxValue, ErrorMessage = "Gross amount must be positive")]
    public Money GrossAmount { get; init; }

    [Range(0, 100, ErrorMessage = "Tax rate must be between 0 and 100")]
    public decimal? TaxRate { get; init; } // Default 19%

    [Required(ErrorMessage = "Payment date is required")]
    public DateTime PaymentDate { get; init; }

    public string? Notes { get; init; }
    public TransactionId TransactionId { get; init; }

    public RecordInterestCommand(
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

public record RecordInterestResult
{
    public TransactionId TransactionId { get; init; } = null!;
    public Money NetAmount { get; init; } = null!; // After tax
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static RecordInterestResult Success(TransactionId transactionId, Money netAmount) =>
        new() { TransactionId = transactionId, NetAmount = netAmount, IsSuccess = true };

    public static RecordInterestResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
