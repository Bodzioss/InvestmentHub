using MassTransit;
using InvestmentHub.Contracts.Messages;
using InvestmentHub.Domain.ReadModels;
using Marten;

namespace InvestmentHub.Workers.Consumers;

public class InvestmentSoldConsumer : IConsumer<InvestmentSoldMessage>
{
    private readonly IDocumentSession _session;
    private readonly ILogger<InvestmentSoldConsumer> _logger;

    public InvestmentSoldConsumer(IDocumentSession session, ILogger<InvestmentSoldConsumer> logger)
    {
        _session = session;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<InvestmentSoldMessage> context)
    {
        try
        {
            var message = context.Message;
            _logger.LogInformation("Processing InvestmentSoldMessage for Investment {InvestmentId} in Portfolio {PortfolioId}", 
                message.InvestmentId, message.PortfolioId);

            // Load the portfolio read model
            var portfolio = await _session.LoadAsync<PortfolioReadModel>(message.PortfolioId);

            if (portfolio == null)
            {
                _logger.LogWarning("Portfolio {PortfolioId} not found while processing InvestmentSoldMessage", message.PortfolioId);
                return;
            }

            // Recalculate state from active investments for robustness
            // This ensures that even if we missed an event or had drift, we converge to the correct state
            var activeInvestments = await _session.Query<InvestmentReadModel>()
                .Where(x => x.PortfolioId == message.PortfolioId && x.Status == Domain.Enums.InvestmentStatus.Active)
                .ToListAsync();

            var calculatedTotalValue = activeInvestments.Sum(x => x.CurrentValue);
            var calculatedCount = activeInvestments.Count;

            bool hasChanges = false;

            if (portfolio.TotalValue != calculatedTotalValue)
            {
                _logger.LogInformation("Updating Portfolio {PortfolioId} TotalValue from {OldValue} to {NewValue}", 
                    portfolio.Id, portfolio.TotalValue, calculatedTotalValue);
                portfolio.TotalValue = calculatedTotalValue;
                hasChanges = true;
            }

            if (portfolio.InvestmentCount != calculatedCount)
            {
                _logger.LogInformation("Updating Portfolio {PortfolioId} InvestmentCount from {OldCount} to {NewCount}", 
                    portfolio.Id, portfolio.InvestmentCount, calculatedCount);
                portfolio.InvestmentCount = calculatedCount;
                hasChanges = true;
            }

            if (hasChanges)
            {
                portfolio.LastUpdated = message.SaleDate;
                _session.Store(portfolio);
                await _session.SaveChangesAsync();
                _logger.LogInformation("Portfolio {PortfolioId} updated successfully", portfolio.Id);
            }
            else
            {
                _logger.LogInformation("Portfolio {PortfolioId} is already up to date", portfolio.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing InvestmentSoldMessage: {Message}", ex.Message);
            throw; // Rethrow to ensure MassTransit handles the retry/error queue logic
        }
    }
}
