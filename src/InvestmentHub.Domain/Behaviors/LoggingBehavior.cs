using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace InvestmentHub.Domain.Behaviors;

/// <summary>
/// Pipeline behavior for logging all MediatR requests and responses.
/// This behavior wraps around all handlers to provide consistent logging and OpenTelemetry tracing.
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ActivitySource ActivitySource = new("InvestmentHub.MediatR");
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the LoggingBehavior class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles the request by adding logging around the handler execution.
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

        // Create OpenTelemetry activity for MediatR request
        using var activity = ActivitySource.StartActivity($"{requestType}.{requestName}");
        activity?.SetTag("mediatr.request.type", requestType);
        activity?.SetTag("mediatr.request.name", requestName);
        activity?.SetTag("mediatr.request.full_name", typeof(TRequest).FullName ?? requestName);

        // Add Correlation ID to activity if available from parent Activity
        // Correlation ID is set in CorrelationIdMiddleware and propagated automatically
        var correlationId = GetCorrelationIdFromActivity();
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            activity?.SetTag("correlation.id", correlationId);
        }

        // _logger.LogInformation("Starting {RequestType} {RequestName}...", ...); // Removed to reduce noise

        try
        {
            var response = await next();

            activity?.SetStatus(ActivityStatusCode.Ok);

            // Only log completion at Information level for Commands (state changes)
            // Queries are logged at Debug level to avoid flooding logs (e.g. polling)
            if (requestType == "Command")
            {
                _logger.LogInformation(
                    "Completed {RequestType} {RequestName} with {@Response}",
                    requestType,
                    requestName,
                    response);
            }
            else
            {
                _logger.LogDebug(
                    "Completed {RequestType} {RequestName}",
                    requestType,
                    requestName);
            }

            return response;
        }
        catch (OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Ok); // Cancellation is not an error
            activity?.SetTag("cancelled", true);

#pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter
            _logger.LogInformation("{RequestType} {RequestName} was cancelled", requestType, requestName);
#pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error", true);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

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

    /// <summary>
    /// Gets Correlation ID from the current Activity or parent Activity.
    /// Correlation ID is set in CorrelationIdMiddleware and propagated through Activity.Current.
    /// </summary>
    /// <returns>Correlation ID string or null if not found</returns>
    private static string? GetCorrelationIdFromActivity()
    {
        var activity = Activity.Current;
        while (activity != null)
        {
            // Check if Correlation ID is set as a tag in this activity
            var correlationId = activity.GetTagItem("correlation.id")?.ToString();
            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                return correlationId;
            }

            // Move to parent activity
            activity = activity.Parent;
        }

        return null;
    }
}
