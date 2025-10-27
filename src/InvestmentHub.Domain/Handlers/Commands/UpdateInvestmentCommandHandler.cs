using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Repositories;
using MediatR;

namespace InvestmentHub.Domain.Handlers.Commands;

/// <summary>
/// Handler for UpdateInvestmentCommand.
/// Responsible for updating an existing investment with full business logic validation.
/// </summary>
public class UpdateInvestmentCommandHandler : IRequestHandler<UpdateInvestmentCommand, UpdateInvestmentResult>
{
    private readonly IInvestmentRepository _investmentRepository;
    private readonly IPortfolioRepository _portfolioRepository;

    /// <summary>
    /// Initializes a new instance of the UpdateInvestmentCommandHandler class.
    /// </summary>
    /// <param name="investmentRepository">The investment repository</param>
    /// <param name="portfolioRepository">The portfolio repository</param>
    public UpdateInvestmentCommandHandler(
        IInvestmentRepository investmentRepository,
        IPortfolioRepository portfolioRepository)
    {
        _investmentRepository = investmentRepository ?? throw new ArgumentNullException(nameof(investmentRepository));
        _portfolioRepository = portfolioRepository ?? throw new ArgumentNullException(nameof(portfolioRepository));
    }

    /// <summary>
    /// Handles the UpdateInvestmentCommand.
    /// </summary>
    /// <param name="request">The command request</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The result of the operation</returns>
    public async Task<UpdateInvestmentResult> Handle(UpdateInvestmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check for cancellation
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("Cancellation was requested");
            }

            // 1. Load investment
            var investment = await _investmentRepository.GetByIdAsync(request.InvestmentId, cancellationToken);
            if (investment == null)
            {
                return UpdateInvestmentResult.Failure("Investment not found");
            }

            // 2. Validate investment is active
            if (investment.Status != InvestmentStatus.Active)
            {
                return UpdateInvestmentResult.Failure("Cannot update inactive investment");
            }

            // 3. Load portfolio to validate access
            var portfolio = await _portfolioRepository.GetByIdAsync(investment.PortfolioId, cancellationToken);
            if (portfolio == null)
            {
                return UpdateInvestmentResult.Failure("Portfolio not found");
            }

            // 4. Apply updates
            bool hasChanges = false;

            if (request.PurchasePrice != null)
            {
                investment.UpdatePurchasePrice(request.PurchasePrice);
                hasChanges = true;
            }

            if (request.Quantity.HasValue)
            {
                investment.UpdateQuantity(request.Quantity.Value);
                hasChanges = true;
            }

            if (request.CurrentPrice != null)
            {
                investment.UpdateCurrentValue(request.CurrentPrice);
                hasChanges = true;
            }

            if (request.PurchaseDate.HasValue)
            {
                investment.UpdatePurchaseDate(request.PurchaseDate.Value);
                hasChanges = true;
            }

            // 5. Validate at least one field was updated
            if (!hasChanges)
            {
                return UpdateInvestmentResult.Failure("No changes specified");
            }

            // 6. Save changes
            await _investmentRepository.UpdateAsync(investment, cancellationToken);
            await _portfolioRepository.UpdateAsync(portfolio, cancellationToken);

            // 7. Return success
            return UpdateInvestmentResult.Success(request.InvestmentId);
        }
        catch (OperationCanceledException)
        {
            // Re-throw cancellation exceptions
            throw;
        }
        catch (Exception ex)
        {
            return UpdateInvestmentResult.Failure($"Failed to update investment: {ex.Message}");
        }
    }
}
