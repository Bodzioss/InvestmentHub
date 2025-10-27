using MediatR;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace InvestmentHub.Domain.Behaviors;

/// <summary>
/// Pipeline behavior for validating all MediatR requests.
/// This behavior wraps around all handlers to provide automatic validation.
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the ValidationBehavior class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public ValidationBehavior(ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles the request by validating it before passing to the handler.
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

        _logger.LogDebug(
            "Validating {RequestType} {RequestName}",
            requestType,
            requestName);

        // Perform data annotations validation
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(request);
        
        if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
        {
            var errors = validationResults
                .Select(r => r.ErrorMessage)
                .Where(e => !string.IsNullOrEmpty(e))
                .ToList();

            _logger.LogWarning(
                "Validation failed for {RequestType} {RequestName}: {ValidationErrors}",
                requestType,
                requestName,
                string.Join(", ", errors));

            // For now, we'll let the validation errors bubble up
            // In a real application, you might want to return a validation failure result
            throw new ValidationException($"Validation failed: {string.Join(", ", errors)}");
        }

        _logger.LogDebug(
            "Validation passed for {RequestType} {RequestName}",
            requestType,
            requestName);

        return await next();
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
