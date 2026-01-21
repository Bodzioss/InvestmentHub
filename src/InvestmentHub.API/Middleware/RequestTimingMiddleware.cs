using System.Diagnostics;

namespace InvestmentHub.API.Middleware;

/// <summary>
/// Middleware that measures the total processing time of a request and adds it as a Server-Timing header.
/// This allows frontend applications and browser dev tools to visualize backend latency.
/// </summary>
public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestTimingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            // Add Server-Timing header
            // Format: total;dur=123.45;desc="Total Backend Time"
            if (!context.Response.HasStarted)
            {
                context.Response.Headers.Append("Server-Timing", $"total;dur={elapsedMs};desc=\"Backend Processing\"");
            }
        }
    }
}
