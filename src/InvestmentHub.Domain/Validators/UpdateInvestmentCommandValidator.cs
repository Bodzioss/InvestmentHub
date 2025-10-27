using FluentValidation;
using InvestmentHub.Domain.Commands;

namespace InvestmentHub.Domain.Validators;

/// <summary>
/// Validator for UpdateInvestmentCommand using FluentValidation.
/// </summary>
public class UpdateInvestmentCommandValidator : AbstractValidator<UpdateInvestmentCommand>
{
    /// <summary>
    /// Initializes a new instance of the UpdateInvestmentCommandValidator class.
    /// </summary>
    public UpdateInvestmentCommandValidator()
    {
        RuleFor(x => x.InvestmentId)
            .NotEmpty()
            .WithMessage("Investment ID is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero")
            .LessThan(1000000)
            .WithMessage("Quantity cannot exceed 1,000,000")
            .When(x => x.Quantity.HasValue);

        RuleFor(x => x.CurrentPrice)
            .NotNull()
            .WithMessage("Current price is required")
            .When(x => x.CurrentPrice != null);

        RuleFor(x => x.CurrentPrice!.Amount)
            .GreaterThan(0)
            .WithMessage("Current price amount must be greater than zero")
            .LessThan(1000000)
            .WithMessage("Current price amount cannot exceed 1,000,000")
            .When(x => x.CurrentPrice != null);

        RuleFor(x => x.CurrentPrice!.Currency)
            .IsInEnum()
            .WithMessage("Currency must be a valid value")
            .When(x => x.CurrentPrice != null);

        RuleFor(x => x.PurchasePrice)
            .NotNull()
            .WithMessage("Purchase price is required")
            .When(x => x.PurchasePrice != null);

        RuleFor(x => x.PurchasePrice!.Amount)
            .GreaterThan(0)
            .WithMessage("Purchase price amount must be greater than zero")
            .LessThan(1000000)
            .WithMessage("Purchase price amount cannot exceed 1,000,000")
            .When(x => x.PurchasePrice != null);

        RuleFor(x => x.PurchasePrice!.Currency)
            .IsInEnum()
            .WithMessage("Currency must be a valid value")
            .When(x => x.PurchasePrice != null);

        RuleFor(x => x.PurchaseDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Purchase date cannot be in the future")
            .GreaterThan(new DateTime(1900, 1, 1))
            .WithMessage("Purchase date must be after 1900")
            .When(x => x.PurchaseDate.HasValue);
    }
}
