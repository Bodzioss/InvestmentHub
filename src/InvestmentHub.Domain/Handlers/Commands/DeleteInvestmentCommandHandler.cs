using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.ReadModels;
using MediatR;
using Marten;

namespace InvestmentHub.Domain.Handlers.Commands;

/// <summary>
/// Handler for DeleteInvestmentCommand.
/// Responsible for deleting an investment using Event Sourcing with Marten.
/// </summary>
public class DeleteInvestmentCommandHandler : IRequestHandler<DeleteInvestmentCommand, DeleteInvestmentResult>
{
    private readonly IDocumentSession _session;

    public DeleteInvestmentCommandHandler(IDocumentSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public async Task<DeleteInvestmentResult> Handle(DeleteInvestmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Load investment aggregate
            var investment = await _session.Events.AggregateStreamAsync<InvestmentAggregate>(request.InvestmentId.Value, token: cancellationToken);
            
            if (investment == null)
            {
                return DeleteInvestmentResult.Failure("Investment not found");
            }

            // 2. Validate Portfolio Ownership (Optional but recommended)
            // We rely on the Investment Aggregate's state.
            // Command does not contain PortfolioId so we cannot cross-check against request.

            // 3. Logic validation (Signifcant value check)
            if (!request.ForceDelete && investment.Status == InvestmentHub.Domain.Enums.InvestmentStatus.Active)
            {
                 var totalCost = investment.PurchasePrice.Amount * investment.Quantity;
                 var currentValue = investment.CurrentValue.Amount;
                 
                 // If investment has significant value (> 10% of cost), require force delete
                 if (totalCost > 0 && currentValue > totalCost * 0.1m)
                 {
                      return DeleteInvestmentResult.Failure(
                        "Investment has significant value. Use ForceDelete=true to confirm deletion.");
                 }
            }

            // 4. Perform Delete
            investment.Delete(request.Reason ?? "Deleted by user");
            
            // 5. Append events
            // We append to the Investment Stream
            _session.Events.Append(request.InvestmentId.Value, investment.GetUncommittedEvents().ToArray());
            
            // 6. Save changes
            await _session.SaveChangesAsync(cancellationToken);

            return DeleteInvestmentResult.Success(request.InvestmentId);
        }
        catch (Exception ex)
        {
            return DeleteInvestmentResult.Failure($"Failed to delete investment: {ex.Message}");
        }
    }
}
