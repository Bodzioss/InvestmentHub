using Marten;
using Microsoft.AspNetCore.Http;
using InvestmentHub.Domain.Services;

namespace InvestmentHub.Infrastructure.Marten;

/// <summary>
/// Service to enrich Marten document sessions with Correlation ID from HTTP context.
/// This ensures that all events saved through Marten have the Correlation ID in their metadata.
/// </summary>
public class MartenCorrelationIdEnricher : ICorrelationIdEnricher
{
    private const string CorrelationIdHeaderName = "X-Correlation-Id";
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the MartenCorrelationIdEnricher class.
    /// </summary>
    /// <param name="httpContextAccessor">HTTP context accessor to get Correlation ID from response headers</param>
    public MartenCorrelationIdEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>
    /// Enriches the Marten document session with Correlation ID from HTTP context.
    /// This should be called before saving events to ensure Correlation ID is included in event metadata.
    /// </summary>
    /// <param name="session">The Marten document session to enrich</param>
    public void EnrichWithCorrelationId(IDocumentSession session)
    {
        var correlationId = GetCorrelationIdFromHttpContext();
        
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            // Set Correlation ID on the session
            // Marten will automatically add this to event metadata when CorrelationIdEnabled is true
            session.CorrelationId = correlationId;
        }
    }

    /// <summary>
    /// Gets Correlation ID from HTTP context response headers.
    /// CorrelationIdMiddleware sets this header, so we can read it back.
    /// </summary>
    /// <returns>Correlation ID string or null if not found</returns>
    private string? GetCorrelationIdFromHttpContext()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        // Get Correlation ID from response headers (set by CorrelationIdMiddleware)
        if (httpContext.Response.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationIdHeader))
        {
            return correlationIdHeader.ToString();
        }

        return null;
    }
}

