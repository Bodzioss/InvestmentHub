using FluentValidation;
using System.Net;
using System.Text.Json;

namespace InvestmentHub.API.Middleware;

/// <summary>
/// Global exception handler middleware that catches exceptions and returns appropriate HTTP responses.
/// This ensures that validation errors and other exceptions are properly returned to the client.
/// </summary>
public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError;
        var message = "An error occurred while processing your request.";
        var errors = new List<string>();

        switch (exception)
        {
            case ValidationException validationException:
                code = HttpStatusCode.BadRequest;
                message = "Validation failed";
                errors = validationException.Errors
                    .Select(e => e.ErrorMessage)
                    .ToList();
                break;

            // ArgumentNullException must come before ArgumentException
            // because ArgumentNullException inherits from ArgumentException
            case ArgumentNullException argumentNullException:
                code = HttpStatusCode.BadRequest;
                message = argumentNullException.Message;
                break;

            case ArgumentException argumentException:
                code = HttpStatusCode.BadRequest;
                message = argumentException.Message;
                break;

            case InvalidOperationException invalidOperationException:
                code = HttpStatusCode.BadRequest;
                message = invalidOperationException.Message;
                break;

            case KeyNotFoundException:
                code = HttpStatusCode.NotFound;
                message = "The requested resource was not found.";
                break;

            case UnauthorizedAccessException:
                code = HttpStatusCode.Unauthorized;
                message = "You are not authorized to perform this action.";
                break;
        }

        var response = new
        {
            error = message,
            errors = errors.Any() ? errors : null,
            statusCode = (int)code
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}

