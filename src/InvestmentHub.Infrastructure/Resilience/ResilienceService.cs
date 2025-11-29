using InvestmentHub.Domain.Services;
using Microsoft.Extensions.Logging;
using Polly;

namespace InvestmentHub.Infrastructure.Resilience;

/// <summary>
/// Implementation of resilience service using Polly policies
/// </summary>
public class ResilienceService : IResilienceService
{
    private readonly ILogger<ResilienceService> _logger;

    public ResilienceService(ILogger<ResilienceService> logger)
    {
        _logger = logger;
    }

    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var policy = PollyPolicies.GetRetryPolicy<T>();
        return await policy.ExecuteAsync(async ct => await operation(), cancellationToken);
    }

    public async Task<T> ExecuteWithCircuitBreakerAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var policy = PollyPolicies.GetCircuitBreakerPolicy<T>();
        return await policy.ExecuteAsync(async ct => await operation(), cancellationToken);
    }

    public async Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> operation, int timeoutSeconds, CancellationToken cancellationToken = default)
    {
        var policy = PollyPolicies.GetTimeoutPolicy<T>(timeoutSeconds);
        return await policy.ExecuteAsync(async ct => await operation(), cancellationToken);
    }

    public async Task<T> ExecuteWithResilienceAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var policy = PollyPolicies.GetCombinedPolicy<T>();
        return await policy.ExecuteAsync(async ct => await operation(), cancellationToken);
    }

    public async Task<T> ExecuteDatabaseOperationAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing database operation with resilience policies");
        var policy = PollyPolicies.GetDatabasePolicy<T>();
        return await policy.ExecuteAsync(async ct => await operation(), cancellationToken);
    }

    public async Task<T> ExecuteCacheOperationAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing cache operation with resilience policies");
        var policy = PollyPolicies.GetCachePolicy<T>();
        return await policy.ExecuteAsync(async ct => await operation(), cancellationToken);
    }

    public async Task<T> ExecuteMessagingOperationAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing messaging operation with resilience policies");
        var policy = PollyPolicies.GetMessagingPolicy<T>();
        return await policy.ExecuteAsync(async ct => await operation(), cancellationToken);
    }
}
