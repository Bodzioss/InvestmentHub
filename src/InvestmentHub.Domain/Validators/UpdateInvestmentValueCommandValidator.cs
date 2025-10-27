using FluentValidation;
using InvestmentHub.Domain.Commands;

namespace InvestmentHub.Domain.Validators;

/// <summary>
/// Validator for UpdateInvestmentValueCommand using FluentValidation.
/// </summary>
public class UpdateInvestmentValueCommandValidator : AbstractValidator<UpdateInvestmentValueCommand>
{
    /// <summary>
    /// Initializes a new instance of the UpdateInvestmentValueCommandValidator class.
    /// </summary>
    public UpdateInvestmentValueCommandValidator()
    {
        RuleFor(x => x.InvestmentId)
            .NotEmpty()
            .WithMessage("Investment ID is required");

        RuleFor(x => x.CurrentPrice)
            .NotNull()
            .WithMessage("Current price is required");

        RuleFor(x => x.CurrentPrice.Amount)
            .GreaterThan(0)
            .WithMessage("Current price amount must be greater than zero")
            .LessThan(1000000)
            .WithMessage("Current price amount cannot exceed 1,000,000");

        RuleFor(x => x.CurrentPrice.Currency)
            .IsInEnum()
            .WithMessage("Currency must be a valid value");
    }
}
