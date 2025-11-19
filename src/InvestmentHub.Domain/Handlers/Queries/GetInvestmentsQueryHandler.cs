using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.ReadModels;
using InvestmentHub.Domain.ValueObjects;
using MediatR;
using Marten;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Domain.Handlers.Queries;

/// <summary>
/// Handler for GetInvestmentsQuery.
/// Responsible for retrieving investments from the read model with filtering and pagination.
/// </summary>
public class GetInvestmentsQueryHandler : IRequestHandler<GetInvestmentsQuery, GetInvestmentsResult>
{
    private readonly IDocumentSession _session;
    private readonly ILogger<GetInvestmentsQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the GetInvestmentsQueryHandler class.
    /// </summary>
    /// <param name="session">The Marten document session</param>
    /// <param name="logger">The logger</param>
    public GetInvestmentsQueryHandler(
        IDocumentSession session,
        ILogger<GetInvestmentsQueryHandler> logger)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the GetInvestmentsQuery by reading from InvestmentReadModel.
    /// </summary>
    /// <param name="request">The query request</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The result of the operation</returns>
    public async Task<GetInvestmentsResult> Handle(GetInvestmentsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Querying investments for portfolio {PortfolioId}", request.PortfolioId.Value);

            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();

            // 1. Validate portfolio exists (check PortfolioReadModel)
            var portfolio = await _session.Query<PortfolioReadModel>()
                .FirstOrDefaultAsync(p => p.Id == request.PortfolioId.Value, cancellationToken);
            
            if (portfolio == null)
            {
                _logger.LogWarning("Portfolio {PortfolioId} not found", request.PortfolioId.Value);
                return GetInvestmentsResult.Failure("Portfolio not found");
            }

            // 2. Build query for InvestmentReadModel
            var query = _session.Query<InvestmentReadModel>()
                .Where(i => i.PortfolioId == request.PortfolioId.Value);

            // 3. Apply filters
            if (request.AssetTypeFilter.HasValue)
            {
                var assetTypeString = request.AssetTypeFilter.Value.ToString();
                query = query.Where(i => i.AssetType == assetTypeString);
            }

            if (request.StatusFilter.HasValue)
            {
                query = query.Where(i => i.Status == request.StatusFilter.Value);
            }

            if (!request.IncludeInactive)
            {
                query = query.Where(i => i.Status == InvestmentStatus.Active);
            }

            // 4. Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // 5. Apply pagination and execute query
            var readModels = await query
                .OrderByDescending(i => i.LastUpdated)
                .Skip(request.Offset)
                .Take(request.Limit)
                .ToListAsync(cancellationToken);

            // 6. Convert to summary DTOs
            var investmentSummaries = readModels.Select(rm => ConvertToInvestmentSummary(rm)).ToList();

            _logger.LogInformation("Successfully retrieved {Count} investments for portfolio {PortfolioId}", 
                investmentSummaries.Count, request.PortfolioId.Value);

            // 7. Return success
            return GetInvestmentsResult.Success(investmentSummaries, totalCount);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Get investments query cancelled for portfolio {PortfolioId}", request.PortfolioId.Value);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve investments for portfolio {PortfolioId}: {Message}", 
                request.PortfolioId.Value, ex.Message);
            return GetInvestmentsResult.Failure($"Failed to retrieve investments: {ex.Message}");
        }
    }

    /// <summary>
    /// Converts InvestmentReadModel to InvestmentSummary.
    /// </summary>
    private static InvestmentSummary ConvertToInvestmentSummary(InvestmentReadModel readModel)
    {
        var investmentId = new InvestmentId(readModel.Id);
        var symbol = new Symbol(
            readModel.Ticker,
            readModel.Exchange,
            Enum.Parse<AssetType>(readModel.AssetType));
        var currency = Enum.Parse<Currency>(readModel.Currency);
        var purchasePrice = new Money(readModel.PurchasePrice, currency);
        var currentValue = new Money(readModel.CurrentValue, currency);
        var totalCost = new Money(readModel.TotalCost, currency);
        var unrealizedGainLoss = new Money(readModel.UnrealizedProfitLoss, currency);
        var currentPrice = readModel.Quantity > 0 
            ? new Money(readModel.ValuePerUnit, currency)
            : new Money(0, currency);

        return new InvestmentSummary(
            investmentId,
            symbol,
            purchasePrice,
            currentPrice,
            readModel.Quantity,
            readModel.PurchaseDate,
            readModel.Status,
            readModel.LastUpdated,
            totalCost,
            currentValue,
            unrealizedGainLoss,
            readModel.ROIPercentage);
    }
}
