using FluentValidation;
using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.Enums;

namespace InvestmentHub.Domain.Validators;

/// <summary>
/// Validator for AddInvestmentCommand using FluentValidation.
/// </summary>
public class AddInvestmentCommandValidator : AbstractValidator<AddInvestmentCommand>
{
    /// <summary>
    /// Initializes a new instance of the AddInvestmentCommandValidator class.
    /// </summary>
    public AddInvestmentCommandValidator()
    {
        RuleFor(x => x.PortfolioId)
            .NotEmpty()
            .WithMessage("Portfolio ID is required");

        RuleFor(x => x.Symbol)
            .NotNull()
            .WithMessage("Symbol is required");

        RuleFor(x => x.Symbol.Ticker)
            .NotEmpty()
            .WithMessage("Ticker symbol is required")
            .MaximumLength(10)
            .WithMessage("Ticker symbol cannot exceed 10 characters")
            .Matches(@"^[A-Z0-9]+$")
            .WithMessage("Ticker symbol must contain only uppercase letters and numbers");

        RuleFor(x => x.Symbol.Exchange)
            .NotEmpty()
            .WithMessage("Exchange is required")
            .MaximumLength(50)
            .WithMessage("Exchange name cannot exceed 50 characters");

        RuleFor(x => x.Symbol.AssetType)
            .IsInEnum()
            .WithMessage("Asset type must be a valid value");

        RuleFor(x => x.PurchasePrice)
            .NotNull()
            .WithMessage("Purchase price is required");

        RuleFor(x => x.PurchasePrice.Amount)
            .GreaterThan(0)
            .WithMessage("Purchase price must be greater than zero")
            .LessThan(1000000)
            .WithMessage("Purchase price cannot exceed 1,000,000");

        RuleFor(x => x.PurchasePrice.Currency)
            .IsInEnum()
            .WithMessage("Currency must be a valid value");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero")
            .LessThan(1000000)
            .WithMessage("Quantity cannot exceed 1,000,000");

        RuleFor(x => x.PurchaseDate)
            .NotEmpty()
            .WithMessage("Purchase date is required")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Purchase date cannot be in the future")
            .GreaterThan(new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            .WithMessage("Purchase date must be after 1900");
    }
}
