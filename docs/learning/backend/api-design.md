# API Design - REST, Controllers, DTOs

> [!NOTE]
> Projektowanie REST API - controllers, routing, DTOs, mapping, error handling

## Quick Reference

### Controller Structure

```csharp
[ApiController]
[Route("api/[controller]")]
public class PortfolioController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public PortfolioController(IMediator mediator) => _mediator = mediator;
    
    [HttpGet]
    public async Task<IActionResult> GetPortfolios([FromQuery] Guid userId)
    {
        var query = new GetPortfoliosQuery(new OwnerId(userId));
        var result = await _mediator.Send(query);
        return Ok(result);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreatePortfolio([FromBody] CreatePortfolioRequest request)
    {
        var command = MapToCommand(request);
        var result = await _mediator.Send(command);
        
        return result.IsSuccess 
            ? CreatedAtAction(nameof(GetPortfolio), new { id = result.PortfolioId }, result)
            : BadRequest(new { error = result.ErrorMessage });
    }
}
```

### DTOs (Data Transfer Objects)

```csharp
// Request DTO
public record CreatePortfolioRequest(
    Guid OwnerId,
    string Name,
    string? Description,
    Currency Currency
);

// Response DTO
public record PortfolioResponseDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public Currency Currency { get; init; }
    public Money? TotalValue { get; init; }
}
```

### HTTP Status Codes

| Code | Znaczenie | Kiedy używać |
|------|-----------|--------------|
| 200 OK | Sukces | GET, PUT (update successful) |
| 201 Created | Utworzono | POST (resource created) |
| 204 No Content | Sukces bez contentu | DELETE |
| 400 Bad Request | Błąd walidacji | Invalid input |
| 401 Unauthorized | Brak tokenu | Missing JWT |
| 403 Forbidden | Brak uprawnień | No role/permission |
| 404 Not Found | Nie znaleziono | Resource doesn't exist |
| 500 Internal Error | Błąd serwera | Unhandled exception |

---

**Pełna dokumentacja zostanie rozszerzona po implementacji API endpoints.**

**Zobacz też:**
- [Backend Core](./backend-core.md) - Commands, Queries
- [DDD](./ddd.md) - Domain layer
