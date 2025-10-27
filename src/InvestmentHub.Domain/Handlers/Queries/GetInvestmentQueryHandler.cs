using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Repositories;
using MediatR;

namespace InvestmentHub.Domain.Handlers.Queries;

/// <summary>
/// Handler for GetInvestmentQuery.
/// Responsible for retrieving a single investment by ID with full business logic.
/// </summary>
public class GetInvestmentQueryHandler : IRequestHandler<GetInvestmentQuery, GetInvestmentResult>
{
    private readonly IInvestmentRepository _investmentRepository;

    /// <summary>
    /// Initializes a new instance of the GetInvestmentQueryHandler class.
    /// </summary>
    /// <param name="investmentRepository">The investment repository</param>
    public GetInvestmentQueryHandler(IInvestmentRepository investmentRepository)
    {
        _investmentRepository = investmentRepository ?? throw new ArgumentNullException(nameof(investmentRepository));
    }

    /// <summary>
    /// Handles the GetInvestmentQuery.
    /// </summary>
    /// <param name="request">The query request</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The result of the operation</returns>
    public async Task<GetInvestmentResult> Handle(GetInvestmentQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Check for cancellation
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("Cancellation was requested");
            }

            // Validate input
            if (request.InvestmentId == null)
            {
                return GetInvestmentResult.Failure("Investment ID is required");
            }

            // Retrieve the investment
            var investment = await _investmentRepository.GetByIdAsync(request.InvestmentId, cancellationToken);

            if (investment == null)
            {
                return GetInvestmentResult.Failure("Investment not found");
            }

            return GetInvestmentResult.Success(investment);
        }
        catch (OperationCanceledException)
        {
            throw; // Re-throw cancellation exceptions
        }
        catch (Exception ex)
        {
            // Log the exception (in a real application, you'd use proper logging)
            return GetInvestmentResult.Failure($"An error occurred while retrieving the investment: {ex.Message}");
        }
    }
}
