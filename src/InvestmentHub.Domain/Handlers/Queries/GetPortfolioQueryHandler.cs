using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.ReadModels;
using MediatR;
using Marten;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Domain.Handlers.Queries;

/// <summary>
/// Handler for GetPortfolioQuery.
/// Responsible for retrieving portfolio data from the read model using Marten.
/// </summary>
public class GetPortfolioQueryHandler : IRequestHandler<GetPortfolioQuery, GetPortfolioResult>
{
    private readonly IDocumentSession _session;
    private readonly ILogger<GetPortfolioQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the GetPortfolioQueryHandler class.
    /// </summary>
    /// <param name="session">The Marten document session</param>
    /// <param name="logger">The logger</param>
    public GetPortfolioQueryHandler(
        IDocumentSession session,
        ILogger<GetPortfolioQueryHandler> logger)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the GetPortfolioQuery by reading from the PortfolioReadModel.
    /// This is a fast read operation against the denormalized read model.
    /// </summary>
    /// <param name="request">The query request</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The result of the operation</returns>
    public async Task<GetPortfolioResult> Handle(GetPortfolioQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Querying portfolio {PortfolioId} from read model", request.PortfolioId.Value);

            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();
            
            // Load portfolio read model from Marten document store
            // This is a fast query against the denormalized read model
            var portfolio = await _session.LoadAsync<PortfolioReadModel>(request.PortfolioId.Value, cancellationToken);
            
            if (portfolio == null)
            {
                _logger.LogWarning("Portfolio {PortfolioId} not found in read model", request.PortfolioId.Value);
                return GetPortfolioResult.Failure("Portfolio not found");
            }

            // Calculate totals from InvestmentReadModels to ensure accuracy
            var investments = await _session.Query<InvestmentReadModel>()
                .Where(x => x.PortfolioId == request.PortfolioId.Value && x.Status == InvestmentHub.Domain.Enums.InvestmentStatus.Active)
                .ToListAsync(cancellationToken);

            portfolio.TotalValue = investments.Sum(x => x.CurrentValue);
            portfolio.TotalCost = investments.Sum(x => x.TotalCost);
            portfolio.InvestmentCount = investments.Count;

            _logger.LogInformation("Successfully retrieved portfolio {PortfolioId} (Version: {Version})", 
                portfolio.Id, portfolio.AggregateVersion);

            return GetPortfolioResult.Success(portfolio);
        }

        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to retrieve portfolio {PortfolioId}: {Message}", 
                request.PortfolioId.Value, ex.Message);
            return GetPortfolioResult.Failure($"Failed to retrieve portfolio: {ex.Message}");
        }
    }
}
