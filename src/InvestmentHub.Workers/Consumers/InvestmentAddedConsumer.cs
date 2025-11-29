using MassTransit;
using InvestmentHub.Contracts.Messages;
using InvestmentHub.Domain.ReadModels;
using Marten;

namespace InvestmentHub.Workers.Consumers;

public class InvestmentAddedConsumer : IConsumer<InvestmentAddedMessage>
{
    private readonly IDocumentSession _session;
    private readonly ILogger<InvestmentAddedConsumer> _logger;

    public InvestmentAddedConsumer(IDocumentSession session, ILogger<InvestmentAddedConsumer> logger)
    {
        _session = session;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<InvestmentAddedMessage> context)
    {
        try 
        {
            var message = context.Message;
            _logger.LogInformation("Processing InvestmentAddedMessage for Investment {InvestmentId} in Portfolio {PortfolioId}", 
                message.InvestmentId, message.PortfolioId);

            // Load the portfolio read model
            var portfolio = await _session.LoadAsync<PortfolioReadModel>(message.PortfolioId);

            if (portfolio == null)
            {
                _logger.LogWarning("Portfolio {PortfolioId} not found while processing InvestmentAddedMessage", message.PortfolioId);
                return;
            }

            // In a real scenario, we might want to recalculate the total value from scratch
            // or perform other complex logic that we moved away from the synchronous projection.
            // For now, we will just log that we are handling it, as the PortfolioProjection (inline)
            // has likely already updated the basic counts.
            
            // However, to demonstrate the pattern, let's say we want to verify the total value matches
            // the sum of all investments.
            
            var investments = await _session.Query<InvestmentReadModel>()
                .Where(x => x.PortfolioId == message.PortfolioId && x.Status == Domain.Enums.InvestmentStatus.Active)
                .ToListAsync();

            var calculatedTotalValue = investments.Sum(x => x.CurrentValue);

            // Update the portfolio with the recalculated value (eventual consistency)
            // Note: This might conflict with the inline projection if they run very close together,
            // but since this is a consumer, it likely runs slightly later.
            
            // Only update if different (to avoid unnecessary writes)
            if (portfolio.TotalValue != calculatedTotalValue)
            {
                _logger.LogInformation("Updating Portfolio {PortfolioId} TotalValue from {OldValue} to {NewValue}", 
                    portfolio.Id, portfolio.TotalValue, calculatedTotalValue);
                
                portfolio.TotalValue = calculatedTotalValue;
                _session.Store(portfolio);
                await _session.SaveChangesAsync();
            }
            else
            {
                _logger.LogInformation("Portfolio {PortfolioId} TotalValue is already up to date ({TotalValue})", 
                    portfolio.Id, portfolio.TotalValue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FAILED to process InvestmentAddedMessage for Investment {InvestmentId}. Error: {ErrorMessage}", 
                context.Message.InvestmentId, ex.Message);
            
            // Rethrow to trigger MassTransit retry policy (Exponential Backoff)
            // After retries are exhausted, message will move to _error queue (DLQ)
            throw;
        }
    }
}
