using Serilog.Context;
using System.Diagnostics;

namespace InvestmentHub.API.Middleware;

/// <summary>
/// Middleware for generating and propagating Correlation ID across the request pipeline.
/// Correlation ID allows tracking all logs related to a single HTTP request.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-Id";
    private const string CorrelationIdPropertyName = "CorrelationId";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get or generate Correlation ID
        var correlationId = GetOrGenerateCorrelationId(context);

        // Add Correlation ID to response headers for debugging
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        // Add Correlation ID to Serilog LogContext
        // This ensures all logs within this request will have CorrelationId property
        using (LogContext.PushProperty(CorrelationIdPropertyName, correlationId))
        {
            // Add Correlation ID to OpenTelemetry Activity
            // This ensures Correlation ID is available in all traces and spans for this request
            // Activity.Current is automatically propagated to all child activities
            Activity.Current?.SetTag("correlation.id", correlationId);
            
            // Log that we're processing the request with this Correlation ID
            _logger.LogDebug(
                "Processing request {Method} {Path} with CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                correlationId);

            // Continue with the request pipeline
            await _next(context);
        }
    }

    /// <summary>
    /// Gets Correlation ID from request header or generates a new one.
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Correlation ID string</returns>
    private static string GetOrGenerateCorrelationId(HttpContext context)
    {
        // Check if Correlation ID is already provided in request headers
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationIdHeader) &&
            !string.IsNullOrWhiteSpace(correlationIdHeader))
        {
            return correlationIdHeader.ToString();
        }

        // Generate new Correlation ID (GUID format)
        return Guid.NewGuid().ToString();
    }
}

