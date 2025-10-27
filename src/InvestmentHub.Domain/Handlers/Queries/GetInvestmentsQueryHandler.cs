using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Repositories;
using MediatR;

namespace InvestmentHub.Domain.Handlers.Queries;

/// <summary>
/// Handler for GetInvestmentsQuery.
/// Responsible for retrieving investments for a portfolio with full business logic.
/// </summary>
public class GetInvestmentsQueryHandler : IRequestHandler<GetInvestmentsQuery, GetInvestmentsResult>
{
    private readonly IInvestmentRepository _investmentRepository;
    private readonly IPortfolioRepository _portfolioRepository;

    /// <summary>
    /// Initializes a new instance of the GetInvestmentsQueryHandler class.
    /// </summary>
    /// <param name="investmentRepository">The investment repository</param>
    /// <param name="portfolioRepository">The portfolio repository</param>
    public GetInvestmentsQueryHandler(
        IInvestmentRepository investmentRepository,
        IPortfolioRepository portfolioRepository)
    {
        _investmentRepository = investmentRepository ?? throw new ArgumentNullException(nameof(investmentRepository));
        _portfolioRepository = portfolioRepository ?? throw new ArgumentNullException(nameof(portfolioRepository));
    }

    /// <summary>
    /// Handles the GetInvestmentsQuery.
    /// </summary>
    /// <param name="request">The query request</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The result of the operation</returns>
    public async Task<GetInvestmentsResult> Handle(GetInvestmentsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Check for cancellation
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("Cancellation was requested");
            }

            // 1. Validate portfolio exists
            var portfolio = await _portfolioRepository.GetByIdAsync(request.PortfolioId, cancellationToken);
            if (portfolio == null)
            {
                return GetInvestmentsResult.Failure("Portfolio not found");
            }

            // 2. Load investments for the portfolio
            var investments = await _investmentRepository.GetByPortfolioIdAsync(request.PortfolioId, cancellationToken);

            // 3. Apply filters
            var filteredInvestments = investments.AsQueryable();

            if (request.AssetTypeFilter.HasValue)
            {
                filteredInvestments = filteredInvestments.Where(i => i.Symbol.AssetType == request.AssetTypeFilter.Value);
            }

            if (request.StatusFilter.HasValue)
            {
                filteredInvestments = filteredInvestments.Where(i => i.Status == request.StatusFilter.Value);
            }

            if (!request.IncludeInactive)
            {
                filteredInvestments = filteredInvestments.Where(i => i.Status == InvestmentStatus.Active);
            }

            // 4. Get total count before pagination
            var totalCount = filteredInvestments.Count();

            // 5. Apply pagination
            var paginatedInvestments = filteredInvestments
                .Skip(request.Offset)
                .Take(request.Limit)
                .ToList();

            // 6. Convert to summary DTOs
            var investmentSummaries = paginatedInvestments.Select(i => new InvestmentSummary(
                i.Id,
                i.Symbol,
                i.PurchasePrice,
                i.CurrentValue.Divide(i.Quantity), // Calculate current price per unit
                i.Quantity,
                i.PurchaseDate,
                i.Status,
                i.LastUpdated,
                i.GetTotalCost(),
                i.CurrentValue,
                i.GetUnrealizedGainLoss(),
                i.GetPercentageGainLoss())).ToList();

            // 7. Return success
            return GetInvestmentsResult.Success(investmentSummaries, totalCount);
        }
        catch (OperationCanceledException)
        {
            // Re-throw cancellation exceptions
            throw;
        }
        catch (Exception ex)
        {
            return GetInvestmentsResult.Failure($"Failed to retrieve investments: {ex.Message}");
        }
    }
}
