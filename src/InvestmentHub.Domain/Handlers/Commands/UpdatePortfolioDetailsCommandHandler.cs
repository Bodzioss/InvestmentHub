using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.Common;
using InvestmentHub.Domain.Commands;
using MediatR;
using Marten;

namespace InvestmentHub.Domain.Handlers.Commands;

public class UpdatePortfolioDetailsCommandHandler : IRequestHandler<UpdatePortfolioDetailsCommand, UpdatePortfolioDetailsResult>
{
    private readonly IDocumentSession _session;

    public UpdatePortfolioDetailsCommandHandler(IDocumentSession session)
    {
        _session = session;
    }

    public async Task<UpdatePortfolioDetailsResult> Handle(UpdatePortfolioDetailsCommand command, CancellationToken cancellationToken)
    {
        var portfolio = await _session.Events.AggregateStreamAsync<PortfolioAggregate>(command.PortfolioId.Value, token: cancellationToken);
        
        if (portfolio == null)
            return UpdatePortfolioDetailsResult.Failure($"Portfolio not found (ID: {command.PortfolioId.Value})");

        try 
        {
            var @event = portfolio.UpdateDetails(command.Name, command.Description);
            
            _session.Events.Append(command.PortfolioId.Value, @event);
            await _session.SaveChangesAsync(cancellationToken);
            
            return UpdatePortfolioDetailsResult.Success();
        }
        catch (Exception ex)
        {
            return UpdatePortfolioDetailsResult.Failure(ex.Message);
        }
    }
}
