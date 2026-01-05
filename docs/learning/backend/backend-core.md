# Backend Core - .NET, CQRS, Event Sourcing, MediatR

> [!NOTE]
> Implementacja CQRS, Event Sourcing i MediatR w projekcie InvestmentHub

## Spis Treści
- [CQRS - Command Query Responsibility Segregation](#cqrs)
- [Event Sourcing](#event-sourcing)
- [MediatR](#mediatr)
- [Commands & Handlers](#commands--handlers)
- [Queries & Handlers](#queries--handlers)
- [Pipeline Behaviors](#pipeline-behaviors)

---

## CQRS

### Co to jest CQRS?

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

**Po co?**
- ✅ Jasny podział operacji zapisu vs odczytu
- ✅ Różne modele danych (write optimized vs read optimized)
- ✅ Skalowalność (osobne bazy dla read/write)
- ✅ Audyt (Event Sourcing dla write)

### Implementacja w InvestmentHub

```
src/InvestmentHub.Domain/
├── Commands/           # Write operations
│   ├── CreatePortfolioCommand.cs
│   ├── AddInvestmentCommand.cs
│   └── ...
├── Queries/            # Read operations
│   ├── GetPortfoliosQuery.cs
│   ├── GetInvestmentsQuery.cs
│   └── ...
├── Handlers/
│   ├── Commands/       # Command handlers
│   └── Queries/        # Query handlers
├── Aggregates/         # Event-sourced aggregates
└── ReadModels/         # Query-optimized models
```

---

## Event Sourcing

### Co to jest?

Zamiast zapisywać **aktualny stan** obiektu, zapisujemy **sekwencję zdarzeń** które doprowadziły do tego stanu.

```
Tradycyjne:  UPDATE portfolios SET name = 'New Name' WHERE id = 1
Event Sourcing: INSERT INTO events VALUES ('PortfolioRenamed', '{name: "New Name"}')
```

### Aggregate Root

Aggregate to grupa obiektów które muszą być spójne. Event Sourcing => każda zmiana to event.

```csharp
// src/InvestmentHub.Domain/Aggregates/PortfolioAggregate.cs
public class PortfolioAggregate
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public Currency Currency { get; private set; }
    public bool IsClosed { get; private set; }
    
    private readonly List<object> _uncommittedEvents = new();
    
    // Factory method - jedyny sposób utworzenia agregatu
    public static PortfolioAggregate Initiate(
        PortfolioId portfolioId,
        OwnerId ownerId,
        string name,
        string? description,
        Currency currency)
    {
        var aggregate = new PortfolioAggregate();
        var @event = new PortfolioInitiatedEvent(
            portfolioId.Value, 
            ownerId.Value,
            name, desc

ription, 
            currency
        );
        
        // Apply zmienia stan agregatu
        aggregate.Apply(@event);
        
        // Dodaj do uncommitted events (do zapisu)
        aggregate._uncommittedEvents.Add(@event);
        
        return aggregate;
    }
    
    // Apply - odtwarza stan z eventu
    public void Apply(PortfolioInitiatedEvent @event)
    {
        Id = @event.PortfolioId;
        Name = @event.Name;
        Currency = @event.Currency;
        IsClosed = false;
    }
    
    // Metody biznesowe generują eventy
    public void Rename(string newName)
    {
        if (IsClosed)
            throw new InvalidOperationException("Cannot rename closed portfolio");
            
        var @event = new PortfolioRenamedEvent(Id, newName);
        Apply(@event);
        _uncommittedEvents.Add(@event);
    }
    
    public PortfolioClosedEvent Close(string reason, OwnerId closedBy)
    {
        if (IsClosed)
            throw new InvalidOperationException("Portfolio is already closed");
            
        var @event = new PortfolioClosedEvent(Id, reason, closedBy.Value);
        Apply(@event);
        _uncommittedEvents.Add(@event);
        return @event;
    }
    
    public void Apply(PortfolioRenamedEvent @event)
    {
        Name = @event.NewName;
    }
    
    public void Apply(PortfolioClosedEvent @event)
    {
        IsClosed = true;
    }
    
    public IEnumerable<object> GetUncommittedEvents() => _uncommittedEvents;
}
```

### Korzyści Event Sourcing

| Korzyść | Opis | Przykład w InvestmentHub |
|---------|------|--------------------------|
| **Audyt** | Pełna historia zmian | Wszystkie transakcje portfela |
| **Debug** | Można odtworzyć dowolny stan | "Co było przed zmianą?" |
| **Time Travel** | "Co by było gdyby" | Symulacje inwestycji |
| **Event Replay** | Przebudowa read modeli | Migracja danych |

---

## MediatR

### Co to jest?

**Mediator pattern** - pośredniczy między kontrolerem a handlerem. Komponenty nie znają się bezpośrednio.

```
Controller → MediatR → Handler
                ↓
          Behaviors (pipeline)
```

**Instalacja:**

```xml
<PackageReference Include="MediatR" Version="12.2.0" />
```

**Rejestracja:**

```csharp
// Program.cs
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(CreatePortfolioCommand).Assembly);
    
    // Pipeline behaviors
    cfg.AddBehavior<IPipelineBehavior, ValidatingBehavior>();
    cfg.AddBehavior<IPipelineBehavior, LoggingBehavior>();
});
```

### Request/Response Pattern

```csharp
// Command implementuje IRequest<TResponse>
public record CreatePortfolioCommand : IRequest<CreatePortfolioResult>
{
    public PortfolioId PortfolioId { get; init; }
    public OwnerId OwnerId { get; init; }
    public string Name { get; init; }
    public string? Description { get; init; }
    public Currency Currency { get; init; }
}

// Result
public record CreatePortfolioResult
{
    public bool IsSuccess { get; init; }
    public PortfolioId? PortfolioId { get; init; }
    public string? ErrorMessage { get; init; }
    
    public static CreatePortfolioResult Success(PortfolioId id) =>
        new() { IsSuccess = true, PortfolioId = id };
        
    public static CreatePortfolioResult Failure(string error) =>
        new() { IsSuccess = false, ErrorMessage = error };
}
```

---

## Commands & Handlers

### Command Pattern

```csharp
// src/InvestmentHub.Domain/Commands/CreatePortfolioCommand.cs
public record CreatePortfolioCommand(
    PortfolioId PortfolioId,
    OwnerId OwnerId,
    string Name,
    string? Description,
    Currency Currency
) : IRequest<CreatePortfolioResult>;
```

**Dlaczego `record`?**
- ✅ Immutable (nie można zmienić po utworzeniu)
- ✅ Value equality (porównuje wartości, nie referencje)
- ✅ Wbudowane `ToString()`, `GetHashCode()`

### Command Handler

```csharp
// src/InvestmentHub.Domain/Handlers/Commands/CreatePortfolioCommandHandler.cs
public class CreatePortfolioCommandHandler 
    : IRequestHandler<CreatePortfolioCommand, CreatePortfolioResult>
{
    private readonly IDocumentSession _session;
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly ILogger<CreatePortfolioCommandHandler> _logger;
    
    public CreatePortfolioCommandHandler(
        IDocumentSession session,
        IPortfolioRepository portfolioRepository,
        ILogger<CreatePortfolioCommandHandler> logger)
    {
        _session = session;
        _portfolioRepository = portfolioRepository;
        _logger = logger;
    }
    
    public async Task<CreatePortfolioResult> Handle(
        CreatePortfolioCommand request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating portfolio {PortfolioId}", 
                request.PortfolioId.Value);
            
            // 1. Walidacja biznesowa
            var existsByName = await _portfolioRepository
                .ExistsByNameAsync(request.OwnerId, request.Name, cancellationToken);
                
            if (existsByName)
                return CreatePortfolioResult.Failure("Portfolio with this name already exists");
            
            // 2. Utworzenie agregatu (generuje event)
            var aggregate = PortfolioAggregate.Initiate(
                request.PortfolioId,
                request.OwnerId,
                request.Name,
                request.Description,
                request.Currency
            );
            
            // 3. Zapis do Event Store
            _session.Events.StartStream<PortfolioAggregate>(
                request.PortfolioId.Value,
                aggregate.GetUncommittedEvents().ToArray()
            );
            
            // 4. Save changes
            await _session.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Successfully created portfolio {PortfolioId}", 
                request.PortfolioId.Value);
            
            return CreatePortfolioResult.Success(request.PortfolioId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create portfolio");
            return CreatePortfolioResult.Failure($"Failed to create portfolio: {ex.Message}");
        }
    }
}
```

**Przepływ:**

```
1. Controller wywołuje:
   await _mediator.Send(new CreatePortfolioCommand(...))
   
2. MediatR znajduje handler:
   CreatePortfolioCommandHandler
   
3. Pipeline Behaviors:
   Logging → Validation → Handler
   
4. Handler:
   - Walidacja biznesowa
   - Tworzenie agregatu
   - Generowanie eventów
   - Zapis do Event Store
   
5. Response zwracany do Controllera
```

---

## Queries & Handlers

### Query Pattern

```csharp
// src/InvestmentHub.Domain/Queries/GetPortfoliosQuery.cs
public record GetPortfoliosQuery : IRequest<IEnumerable<PortfolioReadModel>>
{
    public OwnerId OwnerId { get; init; }
    
    public GetPortfoliosQuery(OwnerId ownerId)
    {
        OwnerId = ownerId;
    }
}
```

### Query Handler

```csharp
// src/InvestmentHub.Domain/Handlers/Queries/GetPortfoliosQueryHandler.cs
public class GetPortfoliosQueryHandler 
    : IRequestHandler<GetPortfoliosQuery, IEnumerable<PortfolioReadModel>>
{
    private readonly IDocumentSession _session;
    private readonly ILogger<GetPortfoliosQueryHandler> _logger;
    
    public GetPortfoliosQueryHandler(
        IDocumentSession session,
        ILogger<GetPortfoliosQueryHandler> logger)
    {
        _session = session;
        _logger = logger;
    }
    
    public async Task<IEnumerable<PortfolioReadModel>> Handle(
        GetPortfoliosQuery request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting portfolios for user {OwnerId}", 
            request.OwnerId.Value);
        
        // Query z Read Model (nie z Event Store!)
        var portfolios = await _session.Query<PortfolioReadModel>()
            .Where(p => p.OwnerId == request.OwnerId.Value)
            .OrderByDescending(p => p.CreatedDate)
            .ToListAsync(cancellationToken);
        
        return portfolios;
    }
}
```

**Read Model vs Aggregate:**

| Aggregate (Write) | Read Model (Read) |
|------------------|-------------------|
| Event-sourced | Denormalized |
| Business logic | Query-optimized |
| Commands | Queries |
| Event Store | PostgreSQL tables |
| Mutable przez eventy | Aktualizowane przez projekcje |

---

## Pipeline Behaviors

### Co to jest?

Pipeline Behavior to middleware który uruchamia się **przed i/lub po** handlerze.

```
Request → Logging → Validation → Performance → Handler → Response
```

### Logging Behavior

```csharp
// src/InvestmentHub.Domain/Behaviors/LoggingBehavior.cs
public class LoggingBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        // PRZED handlerem
        _logger.LogInformation("Handling {RequestName}", requestName);
        
        try
        {
            // Wywołaj handler
            var response = await next();
            
            // PO handlerze (sukces)
            _logger.LogInformation("Handled {RequestName} successfully", requestName);
            
            return response;
        }
        catch (Exception ex)
        {
            // PO handlerze (błąd)
            _logger.LogError(ex, "Error handling {RequestName}", requestName);
            throw;
        }
    }
}
```

### Validation Behavior

```csharp
// src/InvestmentHub.Domain/Behaviors/ValidatingBehavior.cs
public class ValidatingBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    
    public ValidatingBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Zbierz wszystkie błędy walidacji
        var failures = new List<ValidationFailure>();
        
        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            failures.AddRange(result.Errors);
        }
        
        // Jeśli są błędy, rzuć wyjątek
        if (failures.Any())
        {
            throw new ValidationException(failures);
        }
        
        // Walidacja OK - wywołaj handler
        return await next();
    }
}
```

### Performance Behavior

```csharp
// src/InvestmentHub.Domain/Behaviors/PerformanceBehavior.cs
public class PerformanceBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly Stopwatch _timer;
    
    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
        _timer = new Stopwatch();
    }
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _timer.Start();
        
        var response = await next();
        
        _timer.Stop();
        
        var elapsedMs = _timer.ElapsedMilliseconds;
        
        if (elapsedMs > 500) // Ostrzeż jeśli > 500ms
        {
            _logger.LogWarning(
                "{RequestName} took {ElapsedMs}ms to execute",
                typeof(TRequest).Name,
                elapsedMs
            );
        }
        
        return response;
    }
}
```

---

## Przykłady użycia w Controller

```csharp
// src/InvestmentHub.API/Controllers/PortfolioController.cs
[ApiController]
[Route("api/[controller]")]
public class PortfolioController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public PortfolioController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost]
    public async Task<IActionResult> CreatePortfolio(
        [FromBody] CreatePortfolioRequest request,
        CancellationToken cancellationToken)
    {
        // Mapowanie DTO → Command
        var command = new CreatePortfolioCommand(
            PortfolioId.New(),
            new OwnerId(request.OwnerId),
            request.Name,
            request.Description,
            request.Currency
        );
        
        // Wywołaj MediatR
        var result = await _mediator.Send(command, cancellationToken);
        
        // Mapowanie Result → HTTP Response
        if (result.IsSuccess)
        {
            return CreatedAtAction(
                nameof(GetPortfolio),
                new { id = result.PortfolioId!.Value },
                result.PortfolioId
            );
        }
        
        return BadRequest(new { error = result.ErrorMessage });
    }
    
    [HttpGet]
    public async Task<IActionResult> GetPortfolios(
        [FromQuery] Guid userId,
        CancellationToken cancellationToken)
    {
        var query = new GetPortfoliosQuery(new OwnerId(userId));
        var portfolios = await _mediator.Send(query, cancellationToken);
        
        return Ok(portfolios);
    }
}
```

---

## Podsumowanie

### Architektura CQRS + Event Sourcing

```
HTTP Request
      ↓
Controller
      ↓
MediatR
      ↓
Pipeline Behaviors (Logging, Validation, Performance)
      ↓
Command Handler                  Query Handler
      ↓                               ↓
Aggregate (Event Sourcing)      Read Model (PostgreSQL)
      ↓                               ↓
Event Store (Marten)            Direct Query
      ↓
Projections
      ↓
Update Read Models
```

### Best Practices

1. ✅ **Commands są immutable** - użyj `record`
2. ✅ **Handler ma jedną odpowiedzialność** - jeden command/query
3. ✅ **Agregaty chronią invarianty** - walidacja biznesowa w agregacie
4. ✅ **Events są faktem** - nie można ich zmienić
5. ✅ **Queries nie modyfikują stanu** - tylko odczyt
6. ✅ **Pipeline behaviors dla cross-cutting concerns** - logging, validation, performance

### Pytania rekrutacyjne

| Pytanie | Odpowiedź |
|---------|-----------|
| Czym różni się Command od Query? | Command zmienia stan, Query tylko odczytuje |
| Po co Event Sourcing? | Audyt, time travel, event replay, pełna historia |
| Co to jest Aggregate? | Grupa obiektów z invariantami, boundary consistency |
| Po co MediatR? | Mediator pattern, loose coupling, pipeline behaviors |
| Co to jest Pipeline Behavior? | Middleware dla MediatR (logging, validation) |

---

**Następny krok:** [Domain-Driven Design](./ddd.md) - Value Objects, Entities, Aggregates

**Zobacz też:**
- [Infrastructure](./infrastructure.md) - Marten Event Store
- [API Design](./api-design.md) - Controllers, DTOs, Mapping
