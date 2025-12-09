using MassTransit;
using InvestmentHub.Contracts.Messages;
using InvestmentHub.Domain.ReadModels;
using Marten;

namespace InvestmentHub.Workers.Consumers;

public class PortfolioCreatedConsumer : IConsumer<IPortfolioCreatedMessage>
{
    private readonly IDocumentSession _session;
    private readonly ILogger<PortfolioCreatedConsumer> _logger;

    public PortfolioCreatedConsumer(IDocumentSession session, ILogger<PortfolioCreatedConsumer> logger)
    {
        _session = session;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<IPortfolioCreatedMessage> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing PortfolioCreatedMessage for Portfolio {PortfolioId}", message.PortfolioId);

        // Check if portfolio already exists (idempotency)
        var existingPortfolio = await _session.LoadAsync<PortfolioReadModel>(message.PortfolioId);
        if (existingPortfolio != null)
        {
            _logger.LogInformation("Portfolio {PortfolioId} already exists, skipping creation", message.PortfolioId);
            return;
        }

        var portfolio = new PortfolioReadModel
        {
            Id = message.PortfolioId,
            OwnerId = message.OwnerId,
            Name = message.Name,
            Description = message.Description ?? string.Empty,
            InvestmentCount = 0,
            TotalValue = 0,
            LastUpdated = message.CreatedAt
        };

        _session.Store(portfolio);
        await _session.SaveChangesAsync();
        
        _logger.LogInformation("Created PortfolioReadModel for Portfolio {PortfolioId}", message.PortfolioId);
    }
}
