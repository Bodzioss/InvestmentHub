using Marten;
using Marten.Events.Projections;
using Microsoft.Extensions.DependencyInjection;
using InvestmentHub.Infrastructure.Projections;

namespace InvestmentHub.Infrastructure.Marten;

public class ConfigureMartenProjections : IConfigureMarten
{
    public void Configure(IServiceProvider services, StoreOptions options)
    {
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        options.Projections.Add(new MassTransitOutboxProjection(scopeFactory), ProjectionLifecycle.Async);
    }
}
