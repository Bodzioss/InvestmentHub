using FluentValidation;
using InvestmentHub.Domain.Commands;

namespace InvestmentHub.Domain.Validators;

/// <summary>
/// Validator for DeleteInvestmentCommand using FluentValidation.
/// </summary>
public class DeleteInvestmentCommandValidator : AbstractValidator<DeleteInvestmentCommand>
{
    /// <summary>
    /// Initializes a new instance of the DeleteInvestmentCommandValidator class.
    /// </summary>
    public DeleteInvestmentCommandValidator()
    {
        RuleFor(x => x.InvestmentId)
            .NotEmpty()
            .WithMessage("Investment ID is required");
    }
}
