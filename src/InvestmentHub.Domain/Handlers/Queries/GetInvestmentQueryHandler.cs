using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.ReadModels;
using InvestmentHub.Domain.ValueObjects;
using MediatR;
using Marten;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Domain.Handlers.Queries;

/// <summary>
/// Handler for GetInvestmentQuery.
/// Responsible for retrieving a single investment from the read model.
/// </summary>
public class GetInvestmentQueryHandler : IRequestHandler<GetInvestmentQuery, GetInvestmentResult>
{
    private readonly IDocumentSession _session;
    private readonly ILogger<GetInvestmentQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the GetInvestmentQueryHandler class.
    /// </summary>
    /// <param name="session">The Marten document session</param>
    /// <param name="logger">The logger</param>
    public GetInvestmentQueryHandler(
        IDocumentSession session,
        ILogger<GetInvestmentQueryHandler> logger)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the GetInvestmentQuery by reading from InvestmentReadModel.
    /// </summary>
    /// <param name="request">The query request</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The result of the operation</returns>
    public async Task<GetInvestmentResult> Handle(GetInvestmentQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Querying investment {InvestmentId}", request.InvestmentId.Value);

            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();

            // Validate input
            if (request.InvestmentId == null)
            {
                _logger.LogWarning("Investment ID is null");
                return GetInvestmentResult.Failure("Investment ID is required");
            }

            // Query InvestmentReadModel from Marten
            var readModel = await _session.Query<InvestmentReadModel>()
                .FirstOrDefaultAsync(i => i.Id == request.InvestmentId.Value, cancellationToken);

            if (readModel == null)
            {
                _logger.LogWarning("Investment {InvestmentId} not found in read model", request.InvestmentId.Value);
                return GetInvestmentResult.Failure("Investment not found");
            }

            // Convert ReadModel to Investment entity for backwards compatibility
            var investment = ConvertToInvestment(readModel);

            _logger.LogInformation("Successfully retrieved investment {InvestmentId}", request.InvestmentId.Value);
            return GetInvestmentResult.Success(investment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving investment {InvestmentId}: {Message}", 
                request.InvestmentId.Value, ex.Message);
            return GetInvestmentResult.Failure($"An error occurred while retrieving the investment: {ex.Message}");
        }
    }

    /// <summary>
    /// Converts InvestmentReadModel to Investment entity for backwards compatibility.
    /// </summary>
    private static Investment ConvertToInvestment(InvestmentReadModel readModel)
    {
        var investmentId = new InvestmentId(readModel.Id);
        var portfolioId = new PortfolioId(readModel.PortfolioId);
        var symbol = new Symbol(
            readModel.Ticker,
            readModel.Exchange,
            Enum.Parse<AssetType>(readModel.AssetType));
        var purchasePrice = new Money(
            readModel.PurchasePrice,
            Enum.Parse<Currency>(readModel.Currency));
        var currentValue = new Money(
            readModel.CurrentValue,
            Enum.Parse<Currency>(readModel.Currency));

        var investment = new Investment(
            investmentId,
            portfolioId,
            symbol,
            purchasePrice,
            readModel.Quantity,
            readModel.PurchaseDate);

        // Update current value to match read model
        investment.UpdateCurrentValue(currentValue);

        return investment;
    }
}
