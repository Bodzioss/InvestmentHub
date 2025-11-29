using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace InvestmentHub.API.Middleware;

/// <summary>
/// Global exception handler that catches exceptions and converts them to ProblemDetails responses.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path
        };

        switch (exception)
        {
            case ValidationException validationException:
                problemDetails.Title = "Validation failed";
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Detail = "One or more validation errors occurred.";
                problemDetails.Extensions["errors"] = validationException.Errors
                    .Select(e => new { Field = e.PropertyName, Error = e.ErrorMessage })
                    .ToList();
                break;

            case ArgumentNullException argumentNullException:
                problemDetails.Title = "Invalid Argument";
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Detail = argumentNullException.Message;
                break;

            case ArgumentException argumentException:
                problemDetails.Title = "Invalid Argument";
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Detail = argumentException.Message;
                break;

            case InvalidOperationException invalidOperationException:
                problemDetails.Title = "Invalid Operation";
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Detail = invalidOperationException.Message;
                break;

            case KeyNotFoundException:
                problemDetails.Title = "Resource Not Found";
                problemDetails.Status = StatusCodes.Status404NotFound;
                problemDetails.Detail = "The requested resource was not found.";
                break;

            case UnauthorizedAccessException:
                problemDetails.Title = "Unauthorized";
                problemDetails.Status = StatusCodes.Status401Unauthorized;
                problemDetails.Detail = "You are not authorized to perform this action.";
                break;

            default:
                problemDetails.Title = "An error occurred";
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Detail = "An unexpected error occurred while processing your request.";
                break;
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}

