using Marten;
using Marten.Events;
using Marten.Events.Projections;
using MassTransit;
using InvestmentHub.Domain.Events;
using InvestmentHub.Contracts.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentHub.Infrastructure.Projections;

public class MassTransitOutboxProjection : IProjection
{
    private readonly IServiceScopeFactory _scopeFactory;

    public MassTransitOutboxProjection(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void Apply(IDocumentOperations operations, IReadOnlyList<StreamAction> streams)
    {
        // Not used for async projection
    }

    public async Task ApplyAsync(IDocumentOperations operations, IReadOnlyList<StreamAction> streams, CancellationToken cancellation)
    {
        using var scope = _scopeFactory.CreateScope();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        foreach (var stream in streams)
        {
            foreach (var @event in stream.Events)
            {
                switch (@event.Data)
                {
                    case InvestmentAddedEvent e:
                        await publishEndpoint.Publish<InvestmentAddedMessage>(new 
                        {
                            PortfolioId = e.PortfolioId.Value,
                            InvestmentId = e.InvestmentId.Value,
                            Symbol = e.Symbol.Ticker,
                            PurchasePrice = e.PurchasePrice.Amount,
                            Currency = e.PurchasePrice.Currency.ToString(),
                            e.Quantity,
                            e.PurchaseDate
                        }, cancellation);
                        break;

                    case InvestmentValueUpdatedEvent e:
                        await publishEndpoint.Publish<InvestmentValueUpdatedMessage>(new
                        {
                            InvestmentId = e.InvestmentId.Value,
                            PortfolioId = e.PortfolioId.Value,
                            OldValueAmount = e.OldValue.Amount,
                            OldValueCurrency = e.OldValue.Currency.ToString(),
                            NewValueAmount = e.NewValue.Amount,
                            NewValueCurrency = e.NewValue.Currency.ToString(),
                            e.UpdatedAt,
                            ValueChangeAmount = e.ValueChange.Amount,
                            e.PercentageChange
                        }, cancellation);
                        break;

                    case InvestmentSoldEvent e:
                        await publishEndpoint.Publish<InvestmentSoldMessage>(new
                        {
                            InvestmentId = e.InvestmentId.Value,
                            PortfolioId = e.PortfolioId.Value,
                            SalePriceAmount = e.SalePrice.Amount,
                            SalePriceCurrency = e.SalePrice.Currency.ToString(),
                            e.QuantitySold,
                            e.SaleDate,
                            PurchasePriceAmount = e.PurchasePrice.Amount,
                            PurchasePriceCurrency = e.PurchasePrice.Currency.ToString(),
                            TotalProceedsAmount = e.TotalProceeds.Amount,
                            TotalCostAmount = e.TotalCost.Amount,
                            RealizedProfitLossAmount = e.RealizedProfitLoss.Amount,
                            e.ROIPercentage,
                            e.IsCompleteSale
                        }, cancellation);
                        break;

                    case PortfolioCreatedEvent e:
                        await publishEndpoint.Publish<IPortfolioCreatedMessage>(new
                        {
                            PortfolioId = e.PortfolioId.Value,
                            OwnerId = e.OwnerId.Value,
                            e.Name,
                            e.Description,
                            e.CreatedAt
                        }, cancellation);
                        break;
                }
            }
        }
    }
}
