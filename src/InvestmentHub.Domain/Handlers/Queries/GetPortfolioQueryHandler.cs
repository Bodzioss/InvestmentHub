using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Repositories;
using MediatR;

namespace InvestmentHub.Domain.Handlers.Queries;

/// <summary>
/// Handler for GetPortfolioQuery.
/// Responsible for retrieving portfolio data with full business logic.
/// </summary>
public class GetPortfolioQueryHandler : IRequestHandler<GetPortfolioQuery, GetPortfolioResult>
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IInvestmentRepository _investmentRepository;

    /// <summary>
    /// Initializes a new instance of the GetPortfolioQueryHandler class.
    /// </summary>
    /// <param name="portfolioRepository">The portfolio repository</param>
    /// <param name="investmentRepository">The investment repository</param>
    public GetPortfolioQueryHandler(
        IPortfolioRepository portfolioRepository,
        IInvestmentRepository investmentRepository)
    {
        _portfolioRepository = portfolioRepository ?? throw new ArgumentNullException(nameof(portfolioRepository));
        _investmentRepository = investmentRepository ?? throw new ArgumentNullException(nameof(investmentRepository));
    }

    /// <summary>
    /// Handles the GetPortfolioQuery.
    /// </summary>
    /// <param name="request">The query request</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The result of the operation</returns>
    public async Task<GetPortfolioResult> Handle(GetPortfolioQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();
            
            // 1. Load portfolio from repository
            var portfolio = await _portfolioRepository.GetByIdAsync(request.PortfolioId, cancellationToken);
            if (portfolio == null)
            {
                return GetPortfolioResult.Failure("Portfolio not found");
            }

            // 2. Load investments for the portfolio
            var investments = await _investmentRepository.GetByPortfolioIdAsync(request.PortfolioId, cancellationToken);
            
            // 3. Add investments to portfolio (if not already loaded)
            foreach (var investment in investments)
            {
                if (!portfolio.Investments.Any(i => i.Id == investment.Id))
                {
                    portfolio.AddInvestment(investment);
                }
            }

            // 4. Return success with portfolio
            return GetPortfolioResult.Success(portfolio);
        }
        catch (OperationCanceledException)
        {
            // Re-throw cancellation exceptions
            throw;
        }
        catch (Exception ex)
        {
            return GetPortfolioResult.Failure($"Failed to retrieve portfolio: {ex.Message}");
        }
    }
}
