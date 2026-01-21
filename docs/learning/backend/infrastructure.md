# Infrastructure - PostgreSQL, Marten, Redis, RabbitMQ

> [!NOTE]
> Infrastruktura persistence, caching, messaging w InvestmentHub

## Spis Treści
- [Marten - Event Store](#marten-event-store)
- [PostgreSQL](#postgresql)
- [Redis - Distributed Cache](#redis)
- [RabbitMQ - Message Broker](#rabbitmq)

---

## Marten - Event Store

### Co to jest?

**Marten** to biblioteka .NET która zamienia PostgreSQL w:
- **Document Database** (NoSQL-like)
- **Event Store** (Event Sourcing)

### Instalacja

```xml
<PackageReference Include="Marten" Version="7.0.0" />
<PackageReference Include="Marten.AspNetCore" Version="7.0.0" />
```

### Konfiguracja

```csharp
// Program.cs lub Startup
builder.Services.AddMarten(options =>
{
    options.Connection(connectionString);
    
    // Event Store configuration
    options.Events.StreamIdentity = StreamIdentity.AsGuid;
    
    // Projections (eventy → read models)
    options.Projections.Add<PortfolioProjection>(ProjectionLifecycle.Inline);
    options.Projections.Add<InvestmentProjection>(ProjectionLifecycle.Inline);
    
    // Policies
    options.Policies.AllDocumentsAreMultiTenanted();
})
.UseLightweightSessions()  // Session per request
.AddAsyncDaemon(DaemonMode.Solo);  // Background projections
```

### Zapis Eventów (Command Handler)

```csharp
public class CreatePortfolioCommandHandler 
    : IRequestHandler<CreatePortfolioCommand, CreatePortfolioResult>
{
    private readonly IDocumentSession _session;
    
    public async Task<CreatePortfolioResult> Handle(
        CreatePortfolioCommand request, 
        CancellationToken ct)
    {
        // 1. Tworzysz aggregate
        var aggregate = PortfolioAggregate.Initiate(...);
        
        // 2. Startujesz stream (pierwszy event)
        _session.Events.StartStream<PortfolioAggregate>(
            request.PortfolioId.Value,
            aggregate.GetUncommittedEvents().ToArray()
        );
        
        // 3. Zapisujesz
        await _session.SaveChangesAsync(ct);
        
        return CreatePortfolioResult.Success(request.PortfolioId);
    }
}
```

### Append Event (Update)

```csharp
public async Task Handle(RenamePortfolioCommand request, CancellationToken ct)
{
    // 1. Load aggregate from event stream
    var portfolio = await _session.Events
        .AggregateStreamAsync<PortfolioAggregate>(
            request.PortfolioId.Value, 
            token: ct
        );
    
    // 2. Domain logic (generuje event)
    portfolio.Rename(request.NewName);
    
    // 3. Append event do istniejącego streamu
    _session.Events.Append(
        request.PortfolioId.Value,
        portfolio.GetUncommittedEvents().ToArray()
    );
    
    // 4. Save
    await _session.SaveChangesAsync(ct);
}
```

### Query z Read Model

```csharp
public async Task<IEnumerable<PortfolioReadModel>> Handle(
    GetPortfoliosQuery request, 
    CancellationToken ct)
{
    // Query z Document Store (nie Event Store!)
    var portfolios = await _session.Query<PortfolioReadModel>()
        .Where(p => p.OwnerId == request.OwnerId.Value)
        .OrderByDescending(p => p.CreatedDate)
        .ToListAsync(ct);
    
    return portfolios;
}
```

### Projections - Event → Read Model

```csharp
// src/InvestmentHub.Infrastructure/Projections/PortfolioProjection.cs
public class PortfolioProjection : IProjection
{
    public void Apply(
        IDocumentOperations operations, 
        IReadOnlyList<StreamAction> streams)
    {
        foreach (var stream in streams)
        {
            foreach (var @event in stream.Events)
            {
                switch (@event.Data)
                {
                    case PortfolioInitiatedEvent initiated:
                        // CREATE read model
                        operations.Store(new PortfolioReadModel
                        {
                            Id = initiated.PortfolioId,
                            Owner Id = initiated.OwnerId,
                            Name = initiated.Name,
                            // ...
                        });
                        break;
                    
                    case PortfolioRenamedEvent renamed:
                        // UPDATE read model
                        operations.Update<PortfolioReadModel>(
                            renamed.PortfolioId,
                            portfolio => portfolio.Name = renamed.NewName
                        );
                        break;
                    
                    case PortfolioClosedEvent closed:
                        // DELETE read model
                        operations.Delete<PortfolioReadModel>(closed.PortfolioId);
                        break;
                }
            }
        }
    }
}
```

---

## PostgreSQL

### Użycie w projekcie

1. **Event Store** (Marten) - mt_events, mt_streams
2. **Document Store** (Marten) - mt_doc_*
3. **EF Core** - klasyczne tabele (jeśli potrzebne)

### Connection String

```json
// appsettings.json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Database=investmenthub;Username=postgres;Password=..."
  }
}
```

### Tabele Marten (auto-generated)

```sql
-- Event Store
mt_events          -- Wszystkie eventy
mt_streams         -- Stream metadata

-- Document Store
mt_doc_portfolioreadmodel
mt_doc_investmentreadmodel
```

---

## Redis - Distributed Cache

### Co to jest?

**Redis** to in-memory database używana jako cache dla często pobieranych danych (ceny akcji, instrumenty).

### Instalacja

```xml
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.0" />
```

### Konfiguracja

```csharp
// Program.cs
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration["ConnectionStrings:Redis"];
    options.InstanceName = "InvestmentHub_";
});
```

### Użycie

```csharp
// src/InvestmentHub.Infrastructure/MarketData/YahooMarketDataProvider.cs
public class YahooMarketDataProvider : IMarketDataProvider
{
    private readonly IDistributedCache _cache;
    
    public async Task<MarketPrice?> GetLatestPriceAsync(
        string symbol, 
        CancellationToken ct)
    {
        var cacheKey = $"market:price:{symbol.ToUpper()}";
        
        // 1. Sprawdź cache
        var cached = await _cache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrEmpty(cached))
        {
            return JsonSerializer.Deserialize<MarketPrice>(cached);
        }
        
        // 2. Fetch z Yahoo Finance API
        var price = await FetchFromYahooAsync(symbol, ct);
        
        // 3. Zapisz do cache (TTL: 15 min)
        await _cache.SetStringAsync(
            cacheKey, 
            JsonSerializer.Serialize(price),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
            }, 
            ct
        );
        
        return price;
    }
}
```

---

## RabbitMQ - Message Broker

### Co to jest?

**RabbitMQ** + **MassTransit** - asynchroniczna komunikacja między serwisami.

### Instalacja

```xml
<PackageReference Include="MassTransit.RabbitMQ" Version="8.1.0" />
```

### Konfiguracja

```csharp
// Program.cs
builder.Services.AddMassTransit(x =>
{
    // Rejestruj consumers
    x.AddConsumer<InvestmentAddedConsumer>();
    
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(configuration["ConnectionStrings:RabbitMQ"]);
        
        // Retry policy
        cfg.UseMessageRetry(r => r.Exponential(
            retryLimit: 3,
            minInterval: TimeSpan.FromSeconds(1),
            maxInterval: TimeSpan.FromSeconds(10),
            intervalDelta: TimeSpan.FromSeconds(2)
        ));
        
        cfg.ConfigureEndpoints(context);
    });
});
```

### Publishing Messages

```csharp
public class InvestmentAddedEventHandler
{
    private readonly IPublishEndpoint _publisher;
    
    public async Task Handle(InvestmentAddedEvent @event, CancellationToken ct)
    {
        // Publish message do RabbitMQ
        await _publisher.Publish(new InvestmentAddedMessage
        {
            InvestmentId = @event.InvestmentId,
            PortfolioId = @event.PortfolioId,
            Symbol = @event.Symbol.Ticker,
            Quantity = @event.Quantity
        }, ct);
    }
}
```

### Consuming Messages

```csharp
// src/InvestmentHub.Workers/Consumers/InvestmentAddedConsumer.cs
public class InvestmentAddedConsumer : IConsumer<InvestmentAddedMessage>
{
    private readonly ILogger<InvestmentAddedConsumer> _logger;
    
    public async Task Consume(ConsumeContext<InvestmentAddedMessage> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "Processing new investment {InvestmentId} for portfolio {PortfolioId}",
            message.InvestmentId,
            message.PortfolioId
        );
        
        // Worker logic - np. fetch market data
        await ProcessInvestmentAsync(message);
    }
}
```

---

## Podsumowanie

### Architektura Infrastructure

```
API Layer
    ↓
Domain Layer (CQRS, Aggregates, Events)
    ↓
Infrastructure Layer
    ├── Marten (Event Store + Document DB)
    ├── Redis (Cache)
    └── RabbitMQ (Messaging)
    ↓
PostgreSQL Database
```

### Best Practices

1. ✅ **Marten für Event Sourcing** - eventy niezmienne, projekcje do read models
2. ✅ **Redis dla cache** - często pobierane, rzadko zmieniane dane
3. ✅ **RabbitMQ dla async** - workers, background jobs
4. ✅ **Projections inline** - sync update read models

---

**Zobacz też:**
- [Backend Core](./backend-core.md) - Commands, Events, Handlers
- [DDD](./ddd.md) - Aggregates, Value Objects
