using Microsoft.AspNetCore.Mvc;

namespace InvestmentHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogTestController : ControllerBase
{
    private readonly ILogger<LogTestController> _logger;

    public LogTestController(ILogger<LogTestController> logger)
    {
        _logger = logger;
    }

    [HttpGet("test")]
    public IActionResult TestLogs()
    {
        // Test różnych poziomów logowania
        _logger.LogTrace("To jest log TRACE - najniższy poziom");
        _logger.LogDebug("To jest log DEBUG - informacje diagnostyczne");
        _logger.LogInformation("To jest log INFORMATION - ogólne informacje");
        _logger.LogWarning("To jest log WARNING - ostrzeżenie");
        _logger.LogError("To jest log ERROR - błąd");
        _logger.LogCritical("To jest log CRITICAL - krytyczny błąd");

        // Test strukturalnego logowania
        _logger.LogInformation("Użytkownik {UserId} wykonał akcję {Action} o {Timestamp}", 
            "12345", "TestLogs", DateTime.UtcNow);

        // Test z obiektem
        var userInfo = new { UserId = "12345", Action = "TestLogs", IP = "127.0.0.1" };
        _logger.LogInformation("Szczegóły użytkownika: {@UserInfo}", userInfo);

        // Test z wyjątkiem
        try
        {
            throw new InvalidOperationException("To jest testowy wyjątek dla logowania");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wystąpił błąd podczas testowania logów");
        }

        return Ok(new
        {
            Message = "Logi zostały wygenerowane! Sprawdź konsolę i pliki logów.",
            Timestamp = DateTime.UtcNow,
            LogLevels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical" }
        });
    }

    [HttpGet("structured")]
    public IActionResult TestStructuredLogging()
    {
        using var scope = _logger.BeginScope("TestScope {TestId}", Guid.NewGuid());
        
        _logger.LogInformation("Rozpoczęcie testu strukturalnego logowania");
        
        // Symulacja operacji biznesowej
        var operationId = Guid.NewGuid();
        _logger.LogInformation("Operacja {OperationId} rozpoczęta przez użytkownika {UserId}", 
            operationId, "test-user");

        // Symulacja kroków
        for (int i = 1; i <= 3; i++)
        {
            _logger.LogInformation("Krok {StepNumber} z {TotalSteps} - {StepName}", 
                i, 3, $"Krok_{i}");
            Thread.Sleep(100); // Symulacja pracy
        }

        _logger.LogInformation("Operacja {OperationId} zakończona pomyślnie", operationId);
        
        return Ok(new
        {
            Message = "Strukturalne logowanie przetestowane",
            OperationId = operationId,
            Steps = 3
        });
    }
}

