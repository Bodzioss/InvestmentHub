using MediatR;
using Microsoft.Extensions.Logging;
using FluentValidation;
using FluentValidation.Results;

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
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes a new instance of the ValidationBehavior class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="validators">The validators for the request type</param>
    public ValidationBehavior(
        ILogger<ValidationBehavior<TRequest, TResponse>> logger,
        IEnumerable<IValidator<TRequest>> validators)
    {
        _logger = logger;
        _validators = validators;
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

        // Perform FluentValidation
        var validationFailures = new List<ValidationFailure>();
        
        foreach (var validator in _validators)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            validationFailures.AddRange(validationResult.Errors);
        }

        if (validationFailures.Any())
        {
            var errors = validationFailures
                .Select(f => f.ErrorMessage)
                .ToList();

            _logger.LogWarning(
                "Validation failed for {RequestType} {RequestName}: {ValidationErrors}",
                requestType,
                requestName,
                string.Join(", ", errors));

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
