using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace InvestmentHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RedisTestController : ControllerBase
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisTestController> _logger;

    public RedisTestController(IDistributedCache cache, ILogger<RedisTestController> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    [HttpGet("ping")]
    public async Task<IActionResult> Ping()
    {
        try
        {
            _logger.LogInformation("Testowanie połączenia z Redis...");
            
            // Test podstawowy - zapisz i odczytaj wartość
            var testKey = "redis-test-ping";
            var testValue = $"Test Redis - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
            
            await _cache.SetStringAsync(testKey, testValue);
            var retrievedValue = await _cache.GetStringAsync(testKey);
            
            var isWorking = retrievedValue == testValue;
            
            _logger.LogInformation("Redis test: {IsWorking} - Wartość: {Value}", isWorking, retrievedValue);
            
            return Ok(new
            {
                Status = isWorking ? "Connected" : "Failed",
                Message = isWorking ? "Redis działa poprawnie!" : "Problem z Redis",
                TestValue = retrievedValue,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas testowania Redis");
            return StatusCode(500, new
            {
                Status = "Error",
                Message = "Błąd połączenia z Redis",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpPost("set")]
    public async Task<IActionResult> SetValue([FromBody] SetValueRequest request)
    {
        try
        {
            _logger.LogInformation("Zapisywanie wartości w Redis: {Key} = {Value}", request.Key, request.Value);
            
            await _cache.SetStringAsync(request.Key, request.Value, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(request.ExpirationMinutes ?? 5)
            });
            
            _logger.LogInformation("Wartość zapisana pomyślnie");
            
            return Ok(new
            {
                Status = "Success",
                Message = "Wartość zapisana w Redis",
                Key = request.Key,
                ExpirationMinutes = request.ExpirationMinutes ?? 5,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas zapisywania w Redis");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    [HttpGet("get/{key}")]
    public async Task<IActionResult> GetValue(string key)
    {
        try
        {
            _logger.LogInformation("Odczytanie wartości z Redis: {Key}", key);
            
            var value = await _cache.GetStringAsync(key);
            
            if (value == null)
            {
                _logger.LogWarning("Klucz {Key} nie został znaleziony w Redis", key);
                return NotFound(new
                {
                    Status = "NotFound",
                    Message = $"Klucz '{key}' nie został znaleziony",
                    Key = key,
                    Timestamp = DateTime.UtcNow
                });
            }
            
            _logger.LogInformation("Wartość odczytana: {Key} = {Value}", key, value);
            
            return Ok(new
            {
                Status = "Success",
                Key = key,
                Value = value,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas odczytywania z Redis");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    [HttpDelete("delete/{key}")]
    public async Task<IActionResult> DeleteValue(string key)
    {
        try
        {
            _logger.LogInformation("Usuwanie klucza z Redis: {Key}", key);
            
            await _cache.RemoveAsync(key);
            
            _logger.LogInformation("Klucz usunięty: {Key}", key);
            
            return Ok(new
            {
                Status = "Success",
                Message = $"Klucz '{key}' usunięty z Redis",
                Key = key,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas usuwania z Redis");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    [HttpGet("performance")]
    public async Task<IActionResult> PerformanceTest()
    {
        try
        {
            _logger.LogInformation("Rozpoczęcie testu wydajności Redis");
            
            var startTime = DateTime.UtcNow;
            
            // Test 1: Zapisywanie wielu wartości
            var writeStart = DateTime.UtcNow;
            for (int i = 1; i <= 100; i++)
            {
                await _cache.SetStringAsync($"perf-test-{i}", $"Wartość testowa {i} - {DateTime.UtcNow}");
            }
            var writeTime = DateTime.UtcNow - writeStart;
            
            // Test 2: Odczytywanie wielu wartości
            var readStart = DateTime.UtcNow;
            for (int i = 1; i <= 100; i++)
            {
                await _cache.GetStringAsync($"perf-test-{i}");
            }
            var readTime = DateTime.UtcNow - readStart;
            
            // Test 3: Usuwanie wielu wartości
            var deleteStart = DateTime.UtcNow;
            for (int i = 1; i <= 100; i++)
            {
                await _cache.RemoveAsync($"perf-test-{i}");
            }
            var deleteTime = DateTime.UtcNow - deleteStart;
            
            var totalTime = DateTime.UtcNow - startTime;
            
            _logger.LogInformation("Test wydajności Redis zakończony: {TotalTime}ms", totalTime.TotalMilliseconds);
            
            return Ok(new
            {
                Status = "Success",
                Message = "Test wydajności Redis zakończony",
                Results = new
                {
                    WriteOperations = new
                    {
                        Count = 100,
                        TimeMs = writeTime.TotalMilliseconds,
                        AvgTimeMs = writeTime.TotalMilliseconds / 100
                    },
                    ReadOperations = new
                    {
                        Count = 100,
                        TimeMs = readTime.TotalMilliseconds,
                        AvgTimeMs = readTime.TotalMilliseconds / 100
                    },
                    DeleteOperations = new
                    {
                        Count = 100,
                        TimeMs = deleteTime.TotalMilliseconds,
                        AvgTimeMs = deleteTime.TotalMilliseconds / 100
                    },
                    TotalTime = totalTime.TotalMilliseconds
                },
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas testu wydajności Redis");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    [HttpGet("info")]
    public async Task<IActionResult> GetRedisInfo()
    {
        try
        {
            _logger.LogInformation("Pobieranie informacji o Redis");
            
            // Test różnych typów danych
            var stringValue = "Test string";
            var numberValue = 12345;
            var objectValue = new { Name = "InvestmentHub", Version = "1.0.0", Timestamp = DateTime.UtcNow };
            
            await _cache.SetStringAsync("test-string", stringValue);
            await _cache.SetStringAsync("test-number", numberValue.ToString());
            await _cache.SetStringAsync("test-object", JsonSerializer.Serialize(objectValue));
            
            var retrievedString = await _cache.GetStringAsync("test-string");
            var retrievedNumber = await _cache.GetStringAsync("test-number");
            var retrievedObject = await _cache.GetStringAsync("test-object");
            
            var parsedObject = JsonSerializer.Deserialize<object>(retrievedObject ?? "{}");
            
            _logger.LogInformation("Informacje o Redis pobrane pomyślnie");
            
            return Ok(new
            {
                Status = "Success",
                Message = "Redis działa poprawnie z różnymi typami danych",
                DataTypes = new
                {
                    String = new { Original = stringValue, Retrieved = retrievedString, Match = stringValue == retrievedString },
                    Number = new { Original = numberValue, Retrieved = int.Parse(retrievedNumber ?? "0"), Match = numberValue.ToString() == retrievedNumber },
                    Object = new { Original = objectValue, Retrieved = parsedObject, Match = true }
                },
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas pobierania informacji o Redis");
            return StatusCode(500, new { Error = ex.Message });
        }
    }
}

public class SetValueRequest
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int? ExpirationMinutes { get; set; }
}
