using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.Common;
using InvestmentHub.Domain.Commands;
using MediatR;
using Marten;

namespace InvestmentHub.Domain.Handlers.Commands;

public class DeletePortfolioCommandHandler : IRequestHandler<DeletePortfolioCommand, DeletePortfolioResult>
{
    private readonly IDocumentSession _session;

    public DeletePortfolioCommandHandler(IDocumentSession session)
    {
        _session = session;
    }

    public async Task<DeletePortfolioResult> Handle(DeletePortfolioCommand command, CancellationToken cancellationToken)
    {
        // Load aggregate state from event stream
        var portfolio = await _session.Events.AggregateStreamAsync<PortfolioAggregate>(command.PortfolioId.Value, token: cancellationToken);
        
        if (portfolio == null)
            return DeletePortfolioResult.Failure($"Portfolio not found (ID: {command.PortfolioId.Value})");

        if (portfolio.IsClosed)
            return DeletePortfolioResult.Failure("Portfolio is already deleted");

        try 
        {
            // Call domain logic
            var @event = portfolio.Close("Deleted by user", command.DeletedBy);
            
            // Append event to stream
            _session.Events.Append(command.PortfolioId.Value, @event);
            await _session.SaveChangesAsync(cancellationToken);
            
            return DeletePortfolioResult.Success();
        }
        catch (Exception ex)
        {
            return DeletePortfolioResult.Failure(ex.Message);
        }
    }
}
