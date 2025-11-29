namespace InvestmentHub.Domain.Services;

/// <summary>
/// Service for executing operations with resilience patterns
/// </summary>
public interface IResilienceService
{
    /// <summary>
    /// Execute an operation with retry policy
    /// </summary>
    Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute an operation with circuit breaker
    /// </summary>
    Task<T> ExecuteWithCircuitBreakerAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute an operation with timeout
    /// </summary>
    Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> operation, int timeoutSeconds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute an operation with combined resilience policies (timeout, retry, circuit breaker)
    /// </summary>
    Task<T> ExecuteWithResilienceAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a database operation with appropriate resilience policies
    /// </summary>
    Task<T> ExecuteDatabaseOperationAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a cache operation with appropriate resilience policies
    /// </summary>
    Task<T> ExecuteCacheOperationAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a messaging operation with appropriate resilience policies
    /// </summary>
    Task<T> ExecuteMessagingOperationAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
}
