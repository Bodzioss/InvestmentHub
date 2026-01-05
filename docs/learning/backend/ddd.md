# Domain-Driven Design (DDD)

> [!NOTE]
> Aggregates, Value Objects, Domain Events, Entities - wzorce DDD w InvestmentHub

## Spis Treści
- [Value Objects](#value-objects)
- [Entities](#entities)
- [Aggregates](#aggregates)
- [Domain Events](#domain-events)
- [Repositories](#repositories)

---

## Value Objects

### Co to jest?

**Value Object** to obiekt który jest zdefiniowany przez swoje **wartości**, nie identity. Dwa Value Objects z tymi samymi wartościami są równe.

**Cechy:**
- ✅ Immutable  
- ✅ Value equality (nie reference equality)
- ✅ Self-validating
- ✅ Type-safe

### Przykłady z InvestmentHub

#### Money

```csharp
// src/InvestmentHub.Domain/ValueObjects/Money.cs
public record Money
{
    public decimal Amount { get; }
    public Currency Currency { get; }
    
    public Money(decimal amount, Currency currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        
        Amount = amount;
        Currency = currency;
    }
    
    // Operator overloading
    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
        
        return new Money(a.Amount + b.Amount, a.Currency);
    }
    
    public static Money operator -(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot subtract different currencies");
        
        return new Money(a.Amount - b.Amount, a.Currency);
    }
    
    public static Money operator *(Money money, decimal multiplier)
    {
        return new Money(money.Amount * multiplier, money.Currency);
    }
}
```

**Użycie:**

```csharp
var price = new Money(100, Currency.USD);
var totalCost = price * 5; // 500 USD

var profit = new Money(600, Currency.USD) - price; // 100 USD
```

#### Symbol

```csharp
// src/InvestmentHub.Domain/ValueObjects/Symbol.cs
public record Symbol
{
    public string Ticker { get; }
    public string Name { get; }
    public AssetType AssetType { get; }
    
    public Symbol(string ticker, string name, AssetType assetType)
    {
        if (string.IsNullOrWhiteSpace(ticker))
            throw new ArgumentException("Ticker cannot be empty", nameof(ticker));
        
        if (ticker.Length > 10)
            throw new ArgumentException("Ticker too long", nameof(ticker));
        
        Ticker = ticker.ToUpperInvariant();
        Name = name;
        AssetType = assetType;
    }
}
```

#### PortfolioId, InvestmentId, OwnerId

```csharp
// src/InvestmentHub.Domain/ValueObjects/PortfolioId.cs
public record PortfolioId
{
    public Guid Value { get; }
    
    public PortfolioId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("PortfolioId cannot be empty", nameof(value));
        
        Value = value;
    }
    
    public static PortfolioId New() => new(Guid.NewGuid());
    
    public override string ToString() => Value.ToString();
}
```

**Dlaczego nie zwykły Guid?**

```csharp
// ❌ ŹLE - można pomylić parametry
void TransferMoney(Guid sourceId, Guid targetId) { }
TransferMoney(targetId, sourceId); // Kompiluje się, ale błąd logiczny!

// ✅ DOBRZE - type safety
void TransferMoney(PortfolioId sourceId, PortfolioId targetId) { }
TransferMoney(targetId, sourceId); // Compilation error!
```

---

## Entities

### Co to jest?

**Entity** to obiekt który ma **identity**. Dwa Entity z tym samym ID są tym samym obiektem, nawet jeśli mają różne wartości.

**Cechy:**
- ✅ Ma unique ID
- ✅ Może się zmieniać (mutable)
- ✅ Identity equality (porównanie po ID)

### Przykład - Portfolio Entity (Read Model)

```csharp
// src/InvestmentHub.Domain/ReadModels/PortfolioReadModel.cs
public class PortfolioReadModel
{
    public Guid Id { get; set; }  // Identity
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Currency Currency { get; set; }
    public DateTime CreatedDate { get; set; }
    
    // Calculated fields
    public int ActiveInvestmentCount { get; set; }
    public Money? TotalValue { get; set; }
    public Money? TotalCost { get; set; }
    public Money? UnrealizedGainLoss { get; set; }
    
    // Dwa portfolios z tym samym ID to to samo portfolio
    public override bool Equals(object? obj)
    {
        return obj is PortfolioReadModel other && Id == other.Id;
    }
    
    public override int GetHashCode() => Id.GetHashCode();
}
```

---

## Aggregates

### Co to jest?

**Aggregate** to klaster obiektów (Entity + Value Objects) które są traktowane jako jedna jednostka. **Aggregate Root** to główny Entity który kontroluje dostęp do całego aggregate.

**Zasady:**
- ✅ Tylko Aggregate Root może być modyfikowany z zewnątrz
- ✅ Aggregate Root wymusza invarianty (reguły biznesowe)
- ✅ Zmiana aggregate = event

### PortfolioAggregate

```csharp
// src/InvestmentHub.Domain/Aggregates/PortfolioAggregate.cs
public class PortfolioAggregate
{
    // Identity
    public Guid Id { get; private set; }
    
    // Value Objects
    public string Name { get; private set; } = null!;
    public Currency Currency { get; private set; }
    
    // State
    public bool IsClosed { get; private set; }
    
    // Uncommitted events (do zapisu)
    private readonly List<object> _uncommittedEvents = new();
    
    // ===== Factory Method =====
    public static PortfolioAggregate Initiate(...)
    {
        var aggregate = new PortfolioAggregate();
        var @event = new PortfolioInitiatedEvent(...);
        
        aggregate.Apply(@event);
        aggregate._uncommittedEvents.Add(@event);
        
        return aggregate;
    }
    
    // ===== Business Logic (wymusza invarianty) =====
    public void Rename(string newName)
    {
        // Invariant: Nie można zmienić nazwy zamkniętego portfela
        if (IsClosed)
            throw new InvalidOperationException("Cannot rename closed portfolio");
        
        // Invariant: Nazwa nie może być pusta
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name cannot be empty");
        
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
    
    // ===== Apply Methods (odtwarzanie stanu z eventów) =====
    public void Apply(PortfolioInitiatedEvent @event)
    {
        Id = @event.PortfolioId;
        Name = @event.Name;
        Currency = @event.Currency;
        IsClosed = false;
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

### InvestmentAggregate

```csharp
// src/InvestmentHub.Domain/Aggregates/InvestmentAggregate.cs
public class InvestmentAggregate
{
    public Guid Id { get; private set; }
    public Guid PortfolioId { get; private set; }
    public Symbol Symbol { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public Money PurchasePrice { get; private set; } = null!;
    public DateTime PurchaseDate { get; private set; }
    public InvestmentStatus Status { get; private set; }
    public Money? SalePrice { get; private set; }
    public DateTime? SaleDate { get; private set; }
    
    private readonly List<object> _uncommittedEvents = new();
    
    public static InvestmentAggregate Create(...)
    {
        var aggregate = new InvestmentAggregate();
        var @event = new InvestmentAddedEvent(...);
        
        aggregate.Apply(@event);
        aggregate._uncommittedEvents.Add(@event);
        
        return aggregate;
    }
    
    public void Sell(Money salePrice, decimal? quantity, DateTime saleDate)
    {
        // Invariant: Można sprzedać tylko aktywne inwestycje
        if (Status != InvestmentStatus.Active)
            throw new InvalidOperationException("Cannot sell inactive investment");
        
        // Invariant: Cena sprzedaży musi być pozytywna
        if (salePrice.Amount <= 0)
            throw new ArgumentException("Sale price must be positive");
        
        var quantityToSell = quantity ?? Quantity;
        
        // Invariant: Nie można sprzedać więcej niż posiadamy
        if (quantityToSell > Quantity)
            throw new ArgumentException("Cannot sell more than owned");
        
        var @event = new InvestmentSoldEvent(Id, salePrice, quantityToSell, saleDate);
        Apply(@event);
        _uncommittedEvents.Add(@event);
    }
    
    public void Delete(string reason)
    {
        if (Status == InvestmentStatus.Deleted)
            throw new InvalidOperationException("Investment is already deleted");
        
        var @event = new InvestmentDeletedEvent(Id, reason);
        Apply(@event);
        _uncommittedEvents.Add(@event);
    }
    
    // Apply methods...
}
```

**Aggregate Boundaries:**

```
PortfolioAggregate
    - Wymusza: nazwy unikalne, waluta niezmieniona
    
InvestmentAggregate  
    - Wymusza: ceny > 0, ilość > 0, status transitions
```

---

## Domain Events

### Co to jest?

**Domain Event** to zdarzenie które miało miejsce w domenie. Event **nie może** się zmienić - to fakt historyczny.

### Przykłady

```csharp
// src/InvestmentHub.Domain/Events/PortfolioInitiatedEvent.cs
public record PortfolioInitiatedEvent(
    Guid PortfolioId,
    Guid OwnerId,
    string Name,
    string? Description,
    Currency Currency
);

// src/InvestmentHub.Domain/Events/InvestmentAddedEvent.cs
public record InvestmentAddedEvent(
    Guid InvestmentId,
    Guid PortfolioId,
    Symbol Symbol,
    decimal Quantity,
    Money PurchasePrice,
    DateTime PurchaseDate
);

// src/InvestmentHub.Domain/Events/InvestmentSoldEvent.cs
public record InvestmentSoldEvent(
    Guid InvestmentId,
    Money SalePrice,
    decimal Quantity,
    DateTime SaleDate
);
```

**Dlaczego `record`?**
- ✅ Immutable - nie można zmienić po utworzeniu
- ✅ Compact syntax
- ✅ Value equality

### Event Handlers (Projections)

Eventy aktualizują Read Models przez **projekcje**.

```csharp
// src/InvestmentHub.Infrastructure/Projections/PortfolioProjection.cs
public class PortfolioProjection : IProjection
{
    public void Apply(IDocumentOperations operations, IReadOnlyList<StreamAction> streams)
    {
        foreach (var stream in streams)
        {
            foreach (var @event in stream.Events)
            {
                switch (@event.Data)
                {
                    case PortfolioInitiatedEvent initiated:
                        operations.Store(new PortfolioReadModel
                        {
                            Id = initiated.PortfolioId,
                            OwnerId = initiated.OwnerId,
                            Name = initiated.Name,
                            Description = initiated.Description,
                            Currency = initiated.Currency,
                            CreatedDate = @event.Timestamp
                        });
                        break;
                    
                    case PortfolioRenamedEvent renamed:
                        operations.Update<PortfolioReadModel>(
                            renamed.PortfolioId,
                            portfolio => portfolio.Name = renamed.NewName
                        );
                        break;
                    
                    case PortfolioClosedEvent closed:
                        operations.Delete<PortfolioReadModel>(closed.PortfolioId);
                        break;
                }
            }
        }
    }
}
```

---

## Repositories

### Co to jest?

Repository to abstrakcja nad persistence. Ukrywa szczegóły zapisu/odczytu agregatu.

### Interface

```csharp
// src/InvestmentHub.Domain/Interfaces/IPortfolioRepository.cs
public interface IPortfolioRepository
{
    Task<bool> ExistsByNameAsync(
        OwnerId ownerId, 
        string name, 
        CancellationToken cancellationToken = default
    );
}
```

### Implementacja

```csharp
// src/InvestmentHub.Infrastructure/Repositories/PortfolioRepository.cs
public class PortfolioRepository : IPortfolioRepository
{
    private readonly IDocumentSession _session;
    
    public PortfolioRepository(IDocumentSession session)
    {
        _session = session;
    }
    
    public async Task<bool> ExistsByNameAsync(
        OwnerId ownerId,
        string name,
        CancellationToken cancellationToken = default)
    {
        return await _session.Query<PortfolioReadModel>()
            .AnyAsync(p => p.OwnerId == ownerId.Value && p.Name == name, cancellationToken);
    }
}
```

---

## Podsumowanie

### DDD Building Blocks

| Building Block | Przykład | Charakterystyka |
|---------------|----------|-----------------|
| **Value Object** | Money, Symbol, PortfolioId | Immutable, value equality |
| **Entity** | PortfolioReadModel | Identity, mutable |
| **Aggregate** | PortfolioAggregate | Business logic, invariants |
| **Domain Event** | PortfolioInitiatedEvent | Fact, immutable |
| **Repository** | IPortfolioRepository | Persistence abstraction |

### Best Practices

1. ✅ **Value Objects dla konceptów bez identity** - Money, Symbol
2. ✅ **Aggregates wymuszają invarianty** - nie można złamać reguł biznesowych
3. ✅ **Events są faktami** - przeszły czas, immutable
4. ✅ **Repositories ukrywają persistence** - domain nie zna bazy danych
5. ✅ **Używaj record dla VO i Events** - immutability + compact syntax

---

**Zobacz też:**
- [Backend Core](./backend-core.md) - CQRS, Event Sourcing
- [Infrastructure](./infrastructure.md) - Marten, PostgreSQL, Projections
