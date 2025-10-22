using Microsoft.Extensions.Caching.Distributed;

namespace InvestmentHub.Workers;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IDistributedCache _cache;

    public Worker(ILogger<Worker> logger, IDistributedCache cache)
    {
        _logger = logger;
        _cache = cache;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker rozpoczął działanie");
        
        var iteration = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            iteration++;
            
            // Test różnych poziomów logowania w Worker
            _logger.LogTrace("Worker iteration {Iteration} - Trace level", iteration);
            _logger.LogDebug("Worker iteration {Iteration} - Debug level", iteration);
            _logger.LogInformation("Worker running at: {time} - Iteration {Iteration}", DateTimeOffset.Now, iteration);
            
            if (iteration % 5 == 0)
            {
                _logger.LogWarning("Worker osiągnął {Iteration} iteracji - to jest ostrzeżenie", iteration);
            }
            
            if (iteration % 10 == 0)
            {
                _logger.LogInformation("Worker wykonał {Iteration} iteracji - strukturalne logowanie działa!", iteration);
                
                // Test Redis w Worker
                await TestRedisInWorker(iteration);
            }
            
            await Task.Delay(5000, stoppingToken); // Zmienione na 5 sekund dla lepszego testowania
        }
        
        _logger.LogInformation("Worker zakończył działanie po {Iteration} iteracjach", iteration);
    }

    private async Task TestRedisInWorker(int iteration)
    {
        try
        {
            _logger.LogInformation("Testowanie Redis w Worker - iteracja {Iteration}", iteration);
            
            // Test zapisu do Redis
            var workerKey = $"worker-iteration-{iteration}";
            var workerValue = $"Worker test - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - Iteracja {iteration}";
            
            await _cache.SetStringAsync(workerKey, workerValue, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });
            
            // Test odczytu z Redis
            var retrievedValue = await _cache.GetStringAsync(workerKey);
            
            if (retrievedValue == workerValue)
            {
                _logger.LogInformation("Redis test w Worker - SUKCES: {Key} = {Value}", workerKey, retrievedValue);
            }
            else
            {
                _logger.LogWarning("Redis test w Worker - BŁĄD: Oczekiwano {Expected}, otrzymano {Actual}", workerValue, retrievedValue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas testowania Redis w Worker");
        }
    }
}
