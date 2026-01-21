using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.ReadModels;
using InvestmentHub.Domain.ValueObjects;
using Marten;
using MediatR;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Domain.Handlers.Queries;

/// <summary>
/// Handler for GetUserPortfoliosQuery.
/// Responsible for retrieving all portfolios for a user from the read model using Marten.
/// </summary>
public class GetUserPortfoliosQueryHandler : IRequestHandler<GetUserPortfoliosQuery, GetUserPortfoliosResult>
{
    private readonly IDocumentSession _session;
    private readonly ILogger<GetUserPortfoliosQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the GetUserPortfoliosQueryHandler class.
    /// </summary>
    /// <param name="session">The Marten document session</param>
    /// <param name="logger">The logger</param>
    public GetUserPortfoliosQueryHandler(
        IDocumentSession session,
        ILogger<GetUserPortfoliosQueryHandler> logger)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the GetUserPortfoliosQuery by reading from PortfolioReadModel.
    /// This is a fast read operation against the denormalized read model.
    /// </summary>
    /// <param name="request">The query request</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The result of the operation</returns>
    public async Task<GetUserPortfoliosResult> Handle(GetUserPortfoliosQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Querying portfolios for user {UserId} from read model", request.UserId.Value);

            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();

            // Query portfolios for this user from read model
            var portfolios = await _session.Query<PortfolioReadModel>()
                .Where(p => p.OwnerId == request.UserId.Value && !p.IsClosed) // Filter out closed portfolios
                .ToListAsync(cancellationToken);

            if (!portfolios.Any())
            {
                _logger.LogInformation("No portfolios found for user {UserId}", request.UserId.Value);
                return GetUserPortfoliosResult.Success(new List<PortfolioSummary>());
            }

            // Get all investment read models for these portfolios to calculate totals dynamically
            var portfolioIds = portfolios.Select(p => p.Id).ToList();
            var investments = await _session.Query<InvestmentReadModel>()
                .Where(i => portfolioIds.Contains(i.PortfolioId) && i.Status == InvestmentStatus.Active)
                .ToListAsync(cancellationToken);

            var portfolioSummaries = new List<PortfolioSummary>();

            foreach (var p in portfolios)
            {
                var portfolioInvestments = investments.Where(i => i.PortfolioId == p.Id).ToList();

                // Calculate totals dynamically from active investments
                var totalValueAmount = portfolioInvestments.Sum(i => i.CurrentValue);
                var totalCostAmount = portfolioInvestments.Sum(i => i.PurchasePrice * i.Quantity);
                var totalGainLossAmount = totalValueAmount - totalCostAmount;

                portfolioSummaries.Add(new PortfolioSummary(
                    new PortfolioId(p.Id),
                    p.Name,
                    p.Description,
                    new Money(totalValueAmount, Enum.Parse<Currency>(p.Currency)),
                    new Money(totalCostAmount, Enum.Parse<Currency>(p.Currency)),
                    new Money(totalGainLossAmount, Enum.Parse<Currency>(p.Currency)), // Gain/Loss signed value
                    portfolioInvestments.Count,
                    p.CreatedAt,
                    p.LastUpdated
                ));
            }

            _logger.LogInformation("Successfully retrieved {Count} portfolios for user {UserId}",
                portfolioSummaries.Count, request.UserId.Value);

            return GetUserPortfoliosResult.Success(portfolioSummaries);
        }

        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to retrieve portfolios for user {UserId}: {Message}",
                request.UserId.Value, ex.Message);
            return GetUserPortfoliosResult.Failure($"Failed to retrieve user portfolios: {ex.Message}");
        }
    }
}
