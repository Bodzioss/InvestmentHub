using FluentValidation;
using InvestmentHub.Domain.Commands;

namespace InvestmentHub.Domain.Validators;

/// <summary>
/// Validator for CreatePortfolioCommand using FluentValidation.
/// </summary>
public class CreatePortfolioCommandValidator : AbstractValidator<CreatePortfolioCommand>
{
    /// <summary>
    /// Initializes a new instance of the CreatePortfolioCommandValidator class.
    /// </summary>
    public CreatePortfolioCommandValidator()
    {
        RuleFor(x => x.PortfolioId)
            .NotEmpty()
            .WithMessage("Portfolio ID is required");

        RuleFor(x => x.OwnerId)
            .NotEmpty()
            .WithMessage("Owner ID is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Portfolio name is required")
            .Length(1, 100)
            .WithMessage("Portfolio name must be between 1 and 100 characters")
            .Matches(@"^\S.*\S$|^\S$")
            .WithMessage("Portfolio name cannot be empty or only whitespace");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
