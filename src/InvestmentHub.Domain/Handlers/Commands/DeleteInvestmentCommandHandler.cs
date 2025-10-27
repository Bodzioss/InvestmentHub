using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Repositories;
using MediatR;

namespace InvestmentHub.Domain.Handlers.Commands;

/// <summary>
/// Handler for DeleteInvestmentCommand.
/// Responsible for deleting an investment with full business logic validation.
/// </summary>
public class DeleteInvestmentCommandHandler : IRequestHandler<DeleteInvestmentCommand, DeleteInvestmentResult>
{
    private readonly IInvestmentRepository _investmentRepository;
    private readonly IPortfolioRepository _portfolioRepository;

    /// <summary>
    /// Initializes a new instance of the DeleteInvestmentCommandHandler class.
    /// </summary>
    /// <param name="investmentRepository">The investment repository</param>
    /// <param name="portfolioRepository">The portfolio repository</param>
    public DeleteInvestmentCommandHandler(
        IInvestmentRepository investmentRepository,
        IPortfolioRepository portfolioRepository)
    {
        _investmentRepository = investmentRepository ?? throw new ArgumentNullException(nameof(investmentRepository));
        _portfolioRepository = portfolioRepository ?? throw new ArgumentNullException(nameof(portfolioRepository));
    }

    /// <summary>
    /// Handles the DeleteInvestmentCommand.
    /// </summary>
    /// <param name="request">The command request</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The result of the operation</returns>
    public async Task<DeleteInvestmentResult> Handle(DeleteInvestmentCommand request, CancellationToken cancellationToken)
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
                return DeleteInvestmentResult.Failure("Investment not found");
            }

            // 2. Load portfolio to validate access
            var portfolio = await _portfolioRepository.GetByIdAsync(investment.PortfolioId, cancellationToken);
            if (portfolio == null)
            {
                return DeleteInvestmentResult.Failure("Portfolio not found");
            }

            // 3. Check if investment can be deleted
            if (!request.ForceDelete && investment.Status == InvestmentStatus.Active)
            {
                // Check if investment has significant value
                var totalCost = investment.GetTotalCost();
                var currentValue = investment.CurrentValue;
                
                // If investment has significant value, require force delete
                if (currentValue.Amount > totalCost.Amount * 0.1m) // More than 10% of cost
                {
                    return DeleteInvestmentResult.Failure(
                        "Investment has significant value. Use ForceDelete=true to confirm deletion.");
                }
            }

            // 4. Remove investment from portfolio (this will trigger domain events)
            portfolio.RemoveInvestment(request.InvestmentId);

            // 5. Delete from repository
            await _investmentRepository.RemoveAsync(request.InvestmentId, cancellationToken);
            await _portfolioRepository.UpdateAsync(portfolio, cancellationToken);

            // 6. Log deletion reason if provided
            if (!string.IsNullOrEmpty(request.Reason))
            {
                Console.WriteLine($"Investment {request.InvestmentId} deleted. Reason: {request.Reason}");
            }

            // 7. Return success
            return DeleteInvestmentResult.Success(request.InvestmentId);
        }
        catch (OperationCanceledException)
        {
            // Re-throw cancellation exceptions
            throw;
        }
        catch (Exception ex)
        {
            return DeleteInvestmentResult.Failure($"Failed to delete investment: {ex.Message}");
        }
    }
}
