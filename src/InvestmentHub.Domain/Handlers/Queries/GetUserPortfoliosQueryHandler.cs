using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Repositories;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InvestmentHub.Domain.Handlers.Queries;

/// <summary>
/// Handler for GetUserPortfoliosQuery.
/// Responsible for retrieving all portfolios for a user with full business logic.
/// </summary>
public class GetUserPortfoliosQueryHandler : IRequestHandler<GetUserPortfoliosQuery, GetUserPortfoliosResult>
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// Initializes a new instance of the GetUserPortfoliosQueryHandler class.
    /// </summary>
    /// <param name="portfolioRepository">The portfolio repository</param>
    /// <param name="userRepository">The user repository</param>
    public GetUserPortfoliosQueryHandler(
        IPortfolioRepository portfolioRepository,
        IUserRepository userRepository)
    {
        _portfolioRepository = portfolioRepository ?? throw new ArgumentNullException(nameof(portfolioRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    /// <summary>
    /// Handles the GetUserPortfoliosQuery.
    /// </summary>
    /// <param name="request">The query request</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The result of the operation</returns>
    public async Task<GetUserPortfoliosResult> Handle(GetUserPortfoliosQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();
            
            // 1. Validate user exists
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return GetUserPortfoliosResult.Failure("User not found");
            }

            // 2. Load portfolios for the user
            var portfolios = await _portfolioRepository.GetByUserIdAsync(request.UserId, cancellationToken);

            // 3. Convert to summary DTOs
            var portfolioSummaries = portfolios.Select(p => new PortfolioSummary(
                p.Id,
                p.Name,
                p.Description,
                p.GetTotalValue(),
                p.GetTotalCost(),
                p.GetTotalGainLoss(),
                p.Investments.Count)).ToList();

            // 4. Return success
            return GetUserPortfoliosResult.Success(portfolioSummaries);
        }
        catch (OperationCanceledException)
        {
            // Re-throw cancellation exceptions
            throw;
        }
        catch (Exception ex)
        {
            return GetUserPortfoliosResult.Failure($"Failed to retrieve user portfolios: {ex.Message}");
        }
    }
}
