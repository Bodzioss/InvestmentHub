using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace InvestmentHub.Domain.Behaviors;

/// <summary>
/// Pipeline behavior for logging all MediatR requests and responses.
/// This behavior wraps around all handlers to provide consistent logging.
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
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

        _logger.LogInformation(
            "Starting {RequestType} {RequestName} with {@Request}",
            requestType,
            requestName,
            request);

        try
        {
            var response = await next();
            
            _logger.LogInformation(
                "Completed {RequestType} {RequestName} with {@Response}",
                requestType,
                requestName,
                response);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed {RequestType} {RequestName} with {@Request}",
                requestType,
                requestName,
                request);

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
