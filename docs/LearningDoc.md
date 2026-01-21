# InvestmentHub - LearningDoc - Kompendium Wiedzy dla .NET Developera

> [!NOTE]
> Ten dokument jest żywą dokumentacją - będzie aktualizowany przy każdej nowej funkcji.

---

## Spis treści

### Backend Core
1. [Architektura Projektu](#1-architektura-projektu)
2. [CQRS (Command Query Responsibility Segregation)](#2-cqrs)
3. [Event Sourcing](#3-event-sourcing)
4. [MediatR i Pipeline Behaviors](#4-mediatr)
5. [Dependency Injection](#5-dependency-injection)
6. [Asynchroniczność (async/await)](#6-asynchroniczność)
7. [CORS](#7-cors)
8. [Walidacja (FluentValidation)](#8-walidacja)
9. [Resilience (Polly)](#9-resilience)
10. [Caching (Redis)](#10-caching)
11. [Message Broker (RabbitMQ + MassTransit)](#11-message-broker)
12. [SignalR (Real-time)](#12-signalr)
13. [Background Jobs (Hangfire)](#13-hangfire)
14. [Authentication & Authorization (JWT)](#14-authentication)
15. [Health Checks](#15-health-checks)
16. [Value Objects](#16-value-objects)
17. [Domain-Driven Design (DDD)](#17-ddd)
18. [Marten (PostgreSQL + Event Store)](#18-marten)

### Zaawansowane
19. [Zarządzanie Pamięcią](#19-zarządzanie-pamięcią)
20. [Wzorce Projektowe (Design Patterns)](#20-wzorce-projektowe)

### Infrastruktura
21. [PostgreSQL](#21-postgresql)
22. [Redis](#22-redis)
23. [RabbitMQ](#23-rabbitmq)
24. [Seq (Structured Logging)](#24-seq)
25. [.NET Aspire](#25-net-aspire)
26. [Workers (Background Services)](#26-workers)
27. [Deployment na Azure](#27-deployment-na-azure)

### Frontend
28. [Blazor WebAssembly](#28-blazor-webassembly)

### AI/ML Integration
30. [**AI Financial Analyst (RAG + pgvector)**](./AI_LEARNING.md) ← Osobny plik

### Rekrutacja
29. [Pytania rekrutacyjne](#29-pytania-rekrutacyjne)

---

## 1. Architektura Projektu

### Clean Architecture / Onion Architecture

```
┌─────────────────────────────────────────────┐
│                  API                        │  ← Controllers, Middleware
├─────────────────────────────────────────────┤
│              Application                    │  ← (obecnie puste, logika w Domain)
├─────────────────────────────────────────────┤
│                Domain                       │  ← Agregaty, Eventy, Handlery, ValueObjects
├─────────────────────────────────────────────┤
│             Infrastructure                  │  ← Marten, Redis, RabbitMQ, EF Core
└─────────────────────────────────────────────┘
```

**Zasada zależności:** Warstwy wewnętrzne NIE znają warstw zewnętrznych.
- `Domain` nie wie o `Infrastructure`
- `API` zna wszystkie warstwy

### Struktura folderów

```
src/
├── InvestmentHub.API/           # Warstwa prezentacji
│   ├── Controllers/             # REST endpoints
│   ├── Hubs/                    # SignalR hubs
│   ├── Middleware/              # Custom middleware
│   └── Authorization/           # Polityki autoryzacji
│
├── InvestmentHub.Domain/        # Logika biznesowa
│   ├── Aggregates/              # Aggregate Roots (Event Sourcing)
│   ├── Commands/                # CQRS Commands
│   ├── Queries/                 # CQRS Queries
│   ├── Events/                  # Domain Events
│   ├── Handlers/                # MediatR Handlers
│   ├── ValueObjects/            # Immutable value objects
│   └── Interfaces/              # Porty (Ports)
│
├── InvestmentHub.Infrastructure/ # Implementacje zewnętrzne
│   ├── Data/                    # EF Core, Repositories
│   ├── MarketData/              # Yahoo Finance provider
│   ├── Marten/                  # Event Store configuration
│   └── Jobs/                    # Hangfire jobs
│
└── InvestmentHub.Contracts/     # Shared DTOs
```

---

## 2. CQRS

### Co to jest?

**Command Query Responsibility Segregation** - rozdzielenie operacji zapisu (Commands) od odczytu (Queries).

```
┌─────────────────┐         ┌─────────────────┐
│    Command      │         │     Query       │
│  (Create/Update)│         │    (Read)       │
└────────┬────────┘         └────────┬────────┘
         │                           │
         ▼                           ▼
┌─────────────────┐         ┌─────────────────┐
│  Event Store    │────────▶│  Read Model     │
│  (Marten)       │ Projekcja│  (PostgreSQL)   │
└─────────────────┘         └─────────────────┘
```

### Przykład Command

```csharp
// src/InvestmentHub.Domain/Commands/CreatePortfolioCommand.cs
public record CreatePortfolioCommand(
    PortfolioId PortfolioId,    // Value Object, nie string!
    OwnerId OwnerId,
    string Name,
    string? Description,
    Currency Currency
) : IRequest<CreatePortfolioResult>;  // MediatR interface
```

**Dlaczego `record`?**
- Immutable (nie można zmienić po utworzeniu)
- Value equality (porównuje wartości, nie referencje)
- Wbudowane `ToString()`, `GetHashCode()`

### Handler

```csharp
public class CreatePortfolioCommandHandler 
    : IRequestHandler<CreatePortfolioCommand, CreatePortfolioResult>
{
    private readonly IDocumentSession _session;  // Marten session
    
    public async Task<CreatePortfolioResult> Handle(
        CreatePortfolioCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Walidacja biznesowa
        var existsByName = await _portfolioRepository
            .ExistsByNameAsync(request.OwnerId, request.Name, cancellationToken);
        if (existsByName)
            return CreatePortfolioResult.Failure("Portfolio already exists");
        
        // 2. Utworzenie agregatu (generuje event)
        var aggregate = PortfolioAggregate.Initiate(...);
        
        // 3. Zapis do Event Store
        _session.Events.StartStream<PortfolioAggregate>(
            request.PortfolioId.Value,
            aggregate.GetUncommittedEvents().ToArray());
        
        await _session.SaveChangesAsync(cancellationToken);
        
        return CreatePortfolioResult.Success(request.PortfolioId);
    }
}
```

---

## 3. Event Sourcing

### Co to jest?

Zamiast zapisywać aktualny stan obiektu, zapisujemy **sekwencję zdarzeń** które doprowadziły do tego stanu.

```
Tradycyjne:  UPDATE portfolios SET name = 'New Name' WHERE id = 1
Event Sourcing: INSERT INTO events (stream_id, event_type, data) 
                VALUES (1, 'PortfolioRenamed', '{"newName": "New Name"}')
```

### Aggregate Root

```csharp
public class InvestmentAggregate
{
    public Guid Id { get; private set; }
    public Symbol Symbol { get; private set; } = null!;
    public Money PurchasePrice { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public InvestmentStatus Status { get; private set; }
    
    private readonly List<object> _uncommittedEvents = new();
    
    // Factory method - jedyny sposób utworzenia agregatu
    public static InvestmentAggregate Create(Guid investmentId, ...)
    {
        var aggregate = new InvestmentAggregate();
        var @event = new InvestmentAddedEvent(investmentId, ...);
        aggregate.Apply(@event);
        aggregate._uncommittedEvents.Add(@event);
        return aggregate;
    }
    
    // Apply - odtwarza stan z eventu
    public void Apply(InvestmentAddedEvent @event)
    {
        Id = @event.InvestmentId;
        Symbol = @event.Symbol;
        Status = InvestmentStatus.Active;
    }
    
    // Metody biznesowe generują eventy
    public void Sell(Money salePrice, decimal? quantity, DateTime saleDate)
    {
        if (Status != InvestmentStatus.Active)
            throw new InvalidOperationException("Cannot sell inactive investment");
        
        var @event = new InvestmentSoldEvent(Id, salePrice, quantity ?? Quantity, saleDate);
        Apply(@event);
        _uncommittedEvents.Add(@event);
    }
}
```

### Korzyści Event Sourcing

| Korzyść | Opis |
|---------|------|
| Audyt | Pełna historia zmian |
| Debug | Można odtworzyć dowolny stan |
| Time Travel | "Co by było gdyby" |
| Event Replay | Przebudowa read modeli |

---

## 4. MediatR

### Co to jest?

Implementacja wzorca **Mediator** - pośredniczy między kontrolerem a handlerem.

```
Controller → MediatR → Handler
                ↓
          Behaviors (pipeline)
```

### Pipeline Behavior

```csharp
public class ValidationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // PRZED handlerem
        var failures = new List<ValidationFailure>();
        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            failures.AddRange(result.Errors);
        }
        
        if (failures.Any())
            throw new ValidationException(failures);
        
        // Wywołaj handler
        return await next();
    }
}
```

**Kolejność pipeline:**
```
Request → Logging → Performance → Validation → Handler → Response
```

---

## 5. Dependency Injection

### Lifetimes (cykle życia)

| Lifetime | Opis | Przykład użycia |
|----------|------|-----------------|
| **Singleton** | Jedna instancja na aplikację | Logger, Configuration |
| **Scoped** | Jedna instancja na request HTTP | DbContext, Session |
| **Transient** | Nowa instancja za każdym razem | Lekkie serwisy |

### Przykłady rejestracji

```csharp
// Singleton - jedna instancja na całą aplikację
builder.Services.AddSingleton(new YahooQuotesBuilder().Build());

// Scoped - nowa instancja per HTTP request
builder.Services.AddScoped<IMarketDataProvider, YahooMarketDataProvider>();

// Transient - nowa instancja za każdym razem
builder.Services.AddTransient<IMessageFormatter, DefaultMessageFormatter>();
```

---

## 6. Asynchroniczność

### async/await Podstawy

```csharp
// ŹLE - blokuje wątek
var result = _httpClient.GetAsync(url).Result;

// DOBRZE - zwalnia wątek podczas oczekiwania
var result = await _httpClient.GetAsync(url);
```

### CancellationToken

```csharp
public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    var user = await _repository.GetByIdAsync(request.Id, cancellationToken);
    await _session.SaveChangesAsync(cancellationToken);
}
```

### Parallel vs Concurrent

```csharp
// Sekwencyjne (wolne)
foreach (var id in ids)
    await ProcessAsync(id);

// Równoległe (szybkie)
await Task.WhenAll(ids.Select(id => ProcessAsync(id)));

// Z kontrolą równoległości
await Parallel.ForEachAsync(ids, 
    new ParallelOptions { MaxDegreeOfParallelism = 5 },
    async (id, ct) => await ProcessAsync(id, ct));
```

---

## 7. CORS

### Co to jest?

**Cross-Origin Resource Sharing** - mechanizm bezpieczeństwa przeglądarek.

### Konfiguracja

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// WAŻNE: Kolejność middleware!
app.UseRouting();
app.UseCors("AllowAll");  // MUSI być po UseRouting
app.UseAuthentication();
app.UseAuthorization();
```

---

## 8. Walidacja

### FluentValidation

```csharp
public class AddInvestmentCommandValidator : AbstractValidator<AddInvestmentCommand>
{
    public AddInvestmentCommandValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.PurchasePrice.Amount).GreaterThan(0);
        RuleFor(x => x.Symbol.Ticker).NotEmpty().MaximumLength(10);
        RuleFor(x => x.PurchaseDate).LessThanOrEqualTo(DateTime.UtcNow);
    }
}
```

---

## 9. Resilience (Polly)

### Konfiguracja

```csharp
services.AddResiliencePipeline("default", builder =>
{
    builder
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential  // 1s, 2s, 4s
        })
        .AddTimeout(TimeSpan.FromSeconds(30));
});
```

### Strategie

| Strategia | Kiedy używać |
|-----------|--------------|
| **Retry** | Błędy przejściowe (timeout, 503) |
| **Circuit Breaker** | Gdy serwis jest niedostępny |
| **Timeout** | Ograniczenie czasu oczekiwania |
| **Fallback** | Alternatywna wartość przy błędzie |

---

## 10. Caching

### Distributed Cache (Redis)

```csharp
public async Task<MarketPrice?> GetLatestPriceAsync(string symbol, CancellationToken ct)
{
    var cacheKey = $"market:price:{symbol.ToUpper()}";
    
    // 1. Sprawdź cache
    var cached = await _cache.GetStringAsync(cacheKey, ct);
    if (!string.IsNullOrEmpty(cached))
        return JsonSerializer.Deserialize<MarketPrice>(cached);
    
    // 2. Pobierz z API
    var price = await FetchFromYahoo(symbol, ct);
    
    // 3. Zapisz do cache (15 min TTL)
    await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(price),
        new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
        }, ct);
    
    return price;
}
```

---

## 11. Message Broker

### MassTransit + RabbitMQ

```csharp
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<NotificationConsumer>();
    
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(connectionString);
        cfg.UseMessageRetry(r => r.Exponential(3, 
            TimeSpan.FromSeconds(1), 
            TimeSpan.FromSeconds(10), 
            TimeSpan.FromSeconds(2)));
        cfg.ConfigureEndpoints(context);
    });
});
```

### Consumer

```csharp
public class InvestmentAddedConsumer : IConsumer<InvestmentAddedMessage>
{
    public async Task Consume(ConsumeContext<InvestmentAddedMessage> context)
    {
        var message = context.Message;
        // Aktualizuj Read Model
        _session.Store(new InvestmentReadModel { Id = message.InvestmentId });
        await _session.SaveChangesAsync();
    }
}
```

---

## 12. SignalR

### Hub

```csharp
public class NotificationHub : Hub
{
    public async Task JoinPortfolioGroup(string portfolioId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"portfolio-{portfolioId}");
    }
}
```

### Wysyłanie notyfikacji

```csharp
public async Task NotifyPriceUpdate(Guid portfolioId, decimal newValue)
{
    await _hubContext.Clients
        .Group($"portfolio-{portfolioId}")
        .SendAsync("PriceUpdated", new { portfolioId, newValue });
}
```

---

## 13. Background Jobs (Hangfire)

```csharp
// Konfiguracja
services.AddHangfire(config => config.UsePostgreSqlStorage(connectionString));
services.AddHangfireServer();

// Recurring Job (co 15 minut)
RecurringJob.AddOrUpdate<PriceUpdateJob>("price-update", x => x.Execute(), "*/15 * * * *");
```

---

## 14. Authentication & Authorization

### JWT Token

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidAudience = config["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["Jwt:Key"]!))
        };
    });
```

---

## 15. Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "PostgreSQL")
    .AddRedis(redisConnection, name: "Redis")
    .AddRabbitMQ(rabbitConnection, name: "RabbitMQ");

app.MapHealthChecks("/health");
```

---

## 16. Value Objects

```csharp
public record Money
{
    public decimal Amount { get; }
    public Currency Currency { get; }
    
    public Money(decimal amount, Currency currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative");
        
        Amount = amount;
        Currency = currency;
    }
    
    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
        
        return new Money(a.Amount + b.Amount, a.Currency);
    }
}
```

---

## 17. DDD (Domain-Driven Design)

### Aggregate Root

Aggregate to grupa obiektów które muszą być spójne. Tylko Aggregate Root może być modyfikowany z zewnątrz.

```csharp
// Tylko przez metody agregatu
aggregate.Sell(salePrice, quantity, saleDate);  // ✓

// NIE bezpośrednio
investment.Status = InvestmentStatus.Sold;       // ✗
```

---

## 18. Marten

**Marten** - biblioteka .NET zamieniająca PostgreSQL w Document DB i Event Store.

```csharp
builder.Services.AddMarten(options =>
{
    options.Connection(connectionString);
    options.Events.StreamIdentity = StreamIdentity.AsGuid;
    
    // Projekcje
    options.Projections.Add<InvestmentProjection>(ProjectionLifecycle.Inline);
    options.Projections.Add<PortfolioProjection>(ProjectionLifecycle.Inline);
})
.UseLightweightSessions()
.AddAsyncDaemon(DaemonMode.Solo);
```

---

## 19. Zarządzanie Pamięcią

### Garbage Collector (GC)

```
┌─────────────────────────────────────────────────────┐
│                    Managed Heap                     │
├──────────────┬──────────────┬──────────────────────┤
│  Generation 0│  Generation 1│     Generation 2     │
│  (Krótkoż.)  │  (Średnie)   │   (Długowieczne)     │
└──────────────┴──────────────┴──────────────────────┘
```

**Generacje:**
- **Gen 0:** Nowo utworzone obiekty (zbierane najczęściej)
- **Gen 1:** Przeżyły jedno zbieranie
- **Gen 2:** Przeżyły wiele zbierań (Singleton, static)

### IDisposable i using

```csharp
// ŹLE - wyciek zasobów
var connection = new SqlConnection(connectionString);
connection.Open();

// DOBRZE - using gwarantuje Dispose
using var connection = new SqlConnection(connectionString);
connection.Open();
```

### Memory Leak Patterns (co unikać)

```csharp
// 1. Event handlers bez unsubscribe
bus.Subscribe += OnEvent;  // ⚠️ Memory leak bez Unsubscribe

// 2. Static collections rosnące w nieskończoność
public static List<BigObject> _items = new();  // ⚠️

// 3. Closures trzymające referencje
var bigData = new byte[1_000_000];
_timer.Elapsed += (s, e) => Console.WriteLine(bigData.Length);  // ⚠️
```

### Span<T> (High Performance)

```csharp
// Tradycyjne - alokuje nową tablicę
string hello = text.Substring(0, 5);  // Nowa alokacja

// High-performance - bez alokacji
ReadOnlySpan<char> helloSpan = text.AsSpan().Slice(0, 5);  // Zero alokacji!
```

---

## 20. Wzorce Projektowe

### Factory Method (Aggregate)

```csharp
public class InvestmentAggregate
{
    private InvestmentAggregate() { }  // Prywatny konstruktor
    
    public static InvestmentAggregate Create(...) { }  // Jedyny sposób utworzenia
}
```

### Repository Pattern

```csharp
public interface IPortfolioRepository
{
    Task<Portfolio?> GetByIdAsync(PortfolioId id, CancellationToken ct);
    Task<bool> ExistsByNameAsync(OwnerId ownerId, string name, CancellationToken ct);
}
```

### Mediator (MediatR)

```csharp
// Controller nie zna Handlera - komunikacja przez Mediatora
var result = await _mediator.Send(new CreatePortfolioCommand(...));
```

### Strategy (IMarketDataProvider)

```csharp
public interface IMarketDataProvider { }
public class YahooMarketDataProvider : IMarketDataProvider { }
public class StooqMarketDataProvider : IMarketDataProvider { }  // Planowane
```

---

## 21. PostgreSQL

### Użycie w projekcie

1. **EF Core** - dla prostych CRUD (Users, Instruments)
2. **Marten** - jako Document DB i Event Store

### Konfiguracja (Aspire)

```csharp
var postgres = builder.AddPostgres("postgres").WithDataVolume();

var api = builder.AddProject<Projects.InvestmentHub_API>("api")
    .WithReference(postgres);
```

---

## 22. Redis

### Użycie

- **Cache** - przechowywanie danych tymczasowych
- **Session** - dane sesji użytkownika
- **Pub/Sub** - powiadomienia (SignalR backplane)

### Commands (do debugowania)

```bash
redis-cli
KEYS market:*
GET market:price:AAPL
SETEX market:price:AAPL 900 '{"price": 150.00}'
```

---

## 23. RabbitMQ

### Koncepty

```
Producer → Exchange → Queue → Consumer
```

### Konfiguracja (Aspire)

```csharp
var rabbitmq = builder.AddContainer("rabbitmq", "rabbitmq:3.13-management")
    .WithHttpEndpoint(targetPort: 15672, port: 15672, name: "rabbitmq-management")
    .WithEndpoint(5672, 5672, name: "rabbitmq-amqp", scheme: "amqp");
```

---

## 24. Seq (Structured Logging)

### Structured Logging

```csharp
// ŹLE
_logger.LogInformation($"Created portfolio {portfolioId}");

// DOBRZE - structured
_logger.LogInformation("Created portfolio {PortfolioId}", portfolioId);
```

**Dlaczego structured?**
- Możesz wyszukiwać po `PortfolioId = "abc123"`
- Automatyczne indeksowanie w Seq

---

## 25. .NET Aspire

**.NET Aspire** - orkiestrator do lokalnego developmentu aplikacji rozproszonych.

### AppHost.cs

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres").WithDataVolume();
var redis = builder.AddRedis("redis").WithDataVolume();

var api = builder.AddProject<Projects.InvestmentHub_API>("api")
    .WithReference(postgres)
    .WithReference(redis);

builder.AddProject<Projects.InvestmentHub_Web_Client>("webclient")
    .WithReference(api);

await builder.Build().RunAsync();
```

---

## 26. Workers (Background Services)

Osobny projekt do przetwarzania wiadomości z RabbitMQ.

```csharp
public class InvestmentAddedConsumer : IConsumer<InvestmentAddedMessage>
{
    public async Task Consume(ConsumeContext<InvestmentAddedMessage> context)
    {
        var message = context.Message;
        
        var readModel = new InvestmentReadModel { Id = message.InvestmentId };
        _session.Store(readModel);
        await _session.SaveChangesAsync();
    }
}
```

---

## 27. Deployment na Azure

### Architektura produkcyjna

```
Azure Front Door → Azure Container Apps (API, Workers)
                 → Azure Static Web Apps (Blazor)
                         ↓
              Neon (PostgreSQL) + Upstash (Redis) + CloudAMQP (RabbitMQ)
```

---

## 28. Blazor WebAssembly

### Technologie używane

| Technologia | Do czego |
|-------------|----------|
| **MudBlazor** | UI komponenty (Material Design) |
| **Fluxor** | State management (jak Redux) |
| **Refit** | HTTP client (type-safe API calls) |
| **SignalR Client** | Real-time updates |

### Fluxor (State Management)

```csharp
// State
public record PortfolioState(List<PortfolioDto> Portfolios, bool IsLoading);

// Actions
public record FetchPortfoliosAction(string UserId);
public record FetchPortfoliosSuccessAction(List<PortfolioDto> Portfolios);

// Reducer
[ReducerMethod]
public static PortfolioState ReduceFetchPortfoliosSuccess(PortfolioState state, FetchPortfoliosSuccessAction action)
    => state with { Portfolios = action.Portfolios, IsLoading = false };

// Effect
[EffectMethod]
public async Task HandleFetchPortfolios(FetchPortfoliosAction action, IDispatcher dispatcher)
{
    var portfolios = await _api.GetUserPortfoliosAsync(action.UserId);
    dispatcher.Dispatch(new FetchPortfoliosSuccessAction(portfolios));
}
```

### Refit (Type-safe HTTP)

```csharp
public interface IPortfoliosApi
{
    [Get("/api/portfolios/user/{userId}")]
    Task<List<PortfolioDto>> GetUserPortfoliosAsync(string userId);
    
    [Post("/api/portfolios")]
    Task<PortfolioDto> CreatePortfolioAsync([Body] CreatePortfolioRequest request);
}
```

---

## 29. Pytania rekrutacyjne

### Junior/Mid Level

1. **Czym jest async/await i jak działa?**
2. **Jaka jest różnica między Scoped, Transient i Singleton?**
3. **Co to jest CORS i dlaczego jest potrzebny?**
4. **Jak działa Garbage Collector w .NET?**
5. **Czym jest IDisposable i kiedy go używać?**

### Mid/Senior Level

1. **Wyjaśnij CQRS i kiedy warto go stosować**
2. **Czym jest Event Sourcing i jakie ma zalety/wady?**
3. **Jak zaimplementowałbyś Outbox Pattern?**
4. **Wyjaśnij różnicę między Task.WhenAll a Parallel.ForEach**
5. **Jak zapobiec memory leaks w aplikacji?**

### Senior Level

1. **Jak skalujesz system Event Sourcing?**
2. **Jak obsługujesz eventual consistency?**
3. **Kiedy używać Redis vs PostgreSQL do cache?**
4. **Jak monitorujesz aplikację w produkcji?**
5. **Jak robisz zero-downtime deployment?**

---

## Ćwiczenia do samodzielnego wykonania

| # | Ćwiczenie | Poziom |
|---|-----------|--------|
| 1 | Dodaj nowy validator dla `UpdatePortfolioCommand` | 🟢 Easy |
| 2 | Utwórz nowy Pipeline Behavior (np. CachingBehavior) | 🟡 Medium |
| 3 | Dodaj nowy event `PortfolioArchivedEvent` do agregatu | 🟡 Medium |
| 4 | Zaimplementuj Circuit Breaker dla Yahoo API | 🔴 Hard |
| 5 | Utwórz nowy Consumer w Workers | 🟡 Medium |
| 6 | Dodaj nowy Fluxor Store (np. NotificationsState) | 🟡 Medium |

---

> [!TIP]
> Ten dokument będzie aktualizowany przy każdej nowej funkcji. Sprawdzaj regularnie!
