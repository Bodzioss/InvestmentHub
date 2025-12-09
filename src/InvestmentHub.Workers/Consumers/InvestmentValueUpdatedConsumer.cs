using MassTransit;
using InvestmentHub.Contracts.Messages;
using InvestmentHub.Domain.ReadModels;
using Marten;

namespace InvestmentHub.Workers.Consumers;

public class InvestmentValueUpdatedConsumer : IConsumer<InvestmentValueUpdatedMessage>
{
    private readonly IDocumentSession _session;
    private readonly ILogger<InvestmentValueUpdatedConsumer> _logger;

    public InvestmentValueUpdatedConsumer(IDocumentSession session, ILogger<InvestmentValueUpdatedConsumer> logger)
    {
        _session = session;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<InvestmentValueUpdatedMessage> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing InvestmentValueUpdatedMessage for Investment {InvestmentId}", 
            message.InvestmentId);

        // 1. Update Investment Read Model
        var investment = await _session.LoadAsync<InvestmentReadModel>(message.InvestmentId);
        if (investment == null)
        {
            _logger.LogWarning("Investment {InvestmentId} not found while processing InvestmentValueUpdatedMessage", 
                message.InvestmentId);
            return;
        }

        investment.CurrentValue = message.NewValueAmount;
        investment.LastUpdated = message.UpdatedAt;
        _session.Store(investment);

        _logger.LogInformation("Updated Investment {InvestmentId} CurrentValue from {OldValue} to {NewValue}", 
            message.InvestmentId, message.OldValueAmount, message.NewValueAmount);

        // 2. Recalculate Portfolio Total Value
        var portfolio = await _session.LoadAsync<PortfolioReadModel>(message.PortfolioId);
        if (portfolio == null)
        {
            _logger.LogWarning("Portfolio {PortfolioId} not found while processing InvestmentValueUpdatedMessage", 
                message.PortfolioId);
            await _session.SaveChangesAsync(); // Still save investment update
            return;
        }

        // Recalculate from all active investments for eventual consistency
        var activeInvestments = await _session.Query<InvestmentReadModel>()
            .Where(x => x.PortfolioId == message.PortfolioId && x.Status == Domain.Enums.InvestmentStatus.Active)
            .ToListAsync();

        var calculatedTotalValue = activeInvestments.Sum(x => x.CurrentValue);

        if (portfolio.TotalValue != calculatedTotalValue)
        {
            _logger.LogInformation("Updating Portfolio {PortfolioId} TotalValue from {OldValue} to {NewValue}", 
                portfolio.Id, portfolio.TotalValue, calculatedTotalValue);
            
            portfolio.TotalValue = calculatedTotalValue;
            portfolio.LastUpdated = message.UpdatedAt;
            _session.Store(portfolio);
        }

        await _session.SaveChangesAsync();
        
        _logger.LogInformation("Successfully processed InvestmentValueUpdatedMessage for Investment {InvestmentId}", 
            message.InvestmentId);
    }
}
