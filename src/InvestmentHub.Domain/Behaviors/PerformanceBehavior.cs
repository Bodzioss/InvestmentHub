using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace InvestmentHub.Domain.Behaviors;

/// <summary>
/// Pipeline behavior for measuring performance of all MediatR requests.
/// This behavior wraps around all handlers to provide performance metrics.
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly Stopwatch _stopwatch;

    /// <summary>
    /// Initializes a new instance of the PerformanceBehavior class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
        _stopwatch = new Stopwatch();
    }

    /// <summary>
    /// Handles the request by measuring execution time.
    /// </summary>
    /// <param name="request">The request</param>
    /// <param name="next">The next behavior in the pipeline</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The response</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestType = GetRequestType(request);

        _stopwatch.Restart();

        try
        {
            var response = await next();
            
            _stopwatch.Stop();
            
            var elapsedMilliseconds = _stopwatch.ElapsedMilliseconds;
            
            // Log performance metrics
            if (elapsedMilliseconds > 1000) // More than 1 second
            {
                _logger.LogWarning(
                    "Slow {RequestType} {RequestName} took {ElapsedMilliseconds}ms",
                    requestType,
                    requestName,
                    elapsedMilliseconds);
            }
            else if (elapsedMilliseconds > 500) // More than 500ms
            {
                _logger.LogInformation(
                    "Moderate {RequestType} {RequestName} took {ElapsedMilliseconds}ms",
                    requestType,
                    requestName,
                    elapsedMilliseconds);
            }
            else
            {
                _logger.LogDebug(
                    "Fast {RequestType} {RequestName} took {ElapsedMilliseconds}ms",
                    requestType,
                    requestName,
                    elapsedMilliseconds);
            }

            return response;
        }
        catch (Exception)
        {
            _stopwatch.Stop();
            
            _logger.LogError(
                "Failed {RequestType} {RequestName} after {ElapsedMilliseconds}ms",
                requestType,
                requestName,
                _stopwatch.ElapsedMilliseconds);
            
            throw;
        }
    }

    /// <summary>
    /// Determines the request type (Command or Query) for logging purposes.
    /// </summary>
    /// <param name="request">The request</param>
    /// <returns>The request type string</returns>
    private static string GetRequestType(TRequest request)
    {
        return request switch
        {
            IRequest<object> when request.GetType().Name.EndsWith("Command") => "Command",
            IRequest<object> when request.GetType().Name.EndsWith("Query") => "Query",
            _ => "Request"
        };
    }
}
