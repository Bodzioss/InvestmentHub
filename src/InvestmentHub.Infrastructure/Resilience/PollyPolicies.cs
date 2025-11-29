using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace InvestmentHub.Infrastructure.Resilience;

/// <summary>
/// Centralized Polly policy definitions for resilience patterns
/// </summary>
public static class PollyPolicies
{
    /// <summary>
    /// Retry policy with exponential backoff for transient failures
    /// </summary>
    public static ResiliencePipeline<T> GetRetryPolicy<T>(
        int maxRetryAttempts = 3,
        int initialDelaySeconds = 1,
        int maxDelaySeconds = 10,
        double backoffMultiplier = 2.0)
    {
        return new ResiliencePipelineBuilder<T>()
            .AddRetry(new RetryStrategyOptions<T>
            {
                MaxRetryAttempts = maxRetryAttempts,
                Delay = TimeSpan.FromSeconds(initialDelaySeconds),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                MaxDelay = TimeSpan.FromSeconds(maxDelaySeconds),
                OnRetry = args =>
                {
                    Console.WriteLine($"[Polly Retry] Attempt {args.AttemptNumber} after {args.RetryDelay.TotalSeconds}s delay. Exception: {args.Outcome.Exception?.Message}");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Circuit breaker policy to prevent cascading failures
    /// Opens after consecutive failures, half-open after break duration
    /// </summary>
    public static ResiliencePipeline<T> GetCircuitBreakerPolicy<T>(
        int failureThreshold = 5,
        int durationOfBreakSeconds = 30,
        int minimumThroughput = 10)
    {
        return new ResiliencePipelineBuilder<T>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<T>
            {
                FailureRatio = 0.5, // 50% failure rate
                MinimumThroughput = minimumThroughput,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(durationOfBreakSeconds),
                OnOpened = args =>
                {
                    Console.WriteLine($"[Polly Circuit Breaker] Circuit opened due to {args.Outcome.Exception?.Message}");
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    Console.WriteLine("[Polly Circuit Breaker] Circuit closed - service recovered");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    Console.WriteLine("[Polly Circuit Breaker] Circuit half-open - testing service");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Timeout policy to prevent hanging operations
    /// </summary>
    public static ResiliencePipeline<T> GetTimeoutPolicy<T>(int timeoutSeconds = 30)
    {
        return new ResiliencePipelineBuilder<T>()
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds),
                OnTimeout = args =>
                {
                    Console.WriteLine($"[Polly Timeout] Operation timed out after {timeoutSeconds}s");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Combined policy: Timeout -> Retry -> Circuit Breaker
    /// This is the recommended order for resilience pipelines
    /// </summary>
    public static ResiliencePipeline<T> GetCombinedPolicy<T>(
        int timeoutSeconds = 30,
        int maxRetryAttempts = 3,
        int failureThreshold = 5,
        int durationOfBreakSeconds = 30)
    {
        return new ResiliencePipelineBuilder<T>()
            // 1. Timeout first - prevents hanging operations
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds),
                OnTimeout = args =>
                {
                    Console.WriteLine($"[Polly Timeout] Operation timed out after {timeoutSeconds}s");
                    return ValueTask.CompletedTask;
                }
            })
            // 2. Retry - handles transient failures
            .AddRetry(new RetryStrategyOptions<T>
            {
                MaxRetryAttempts = maxRetryAttempts,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                MaxDelay = TimeSpan.FromSeconds(10),
                OnRetry = args =>
                {
                    Console.WriteLine($"[Polly Retry] Attempt {args.AttemptNumber} after {args.RetryDelay.TotalSeconds}s delay");
                    return ValueTask.CompletedTask;
                }
            })
            // 3. Circuit Breaker - prevents cascading failures
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<T>
            {
                FailureRatio = 0.5,
                MinimumThroughput = 10,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(durationOfBreakSeconds),
                OnOpened = args =>
                {
                    Console.WriteLine("[Polly Circuit Breaker] Circuit opened");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Database-specific policy (longer timeout, aggressive retry)
    /// </summary>
    public static ResiliencePipeline<T> GetDatabasePolicy<T>()
    {
        return GetCombinedPolicy<T>(
            timeoutSeconds: 30,
            maxRetryAttempts: 3,
            failureThreshold: 5,
            durationOfBreakSeconds: 30
        );
    }

    /// <summary>
    /// Cache-specific policy (shorter timeout, minimal retry)
    /// </summary>
    public static ResiliencePipeline<T> GetCachePolicy<T>()
    {
        return GetCombinedPolicy<T>(
            timeoutSeconds: 10,
            maxRetryAttempts: 2,
            failureThreshold: 3,
            durationOfBreakSeconds: 15
        );
    }

    /// <summary>
    /// Messaging-specific policy (moderate timeout, standard retry)
    /// </summary>
    public static ResiliencePipeline<T> GetMessagingPolicy<T>()
    {
        return GetCombinedPolicy<T>(
            timeoutSeconds: 15,
            maxRetryAttempts: 3,
            failureThreshold: 5,
            durationOfBreakSeconds: 30
        );
    }
}
