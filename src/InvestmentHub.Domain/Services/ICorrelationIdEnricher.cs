using Marten;

namespace InvestmentHub.Domain.Services;

/// <summary>
/// Interface for enriching Marten document sessions with Correlation ID.
/// This allows Domain layer to use Correlation ID enrichment without depending on Infrastructure.
/// </summary>
public interface ICorrelationIdEnricher
{
    /// <summary>
    /// Enriches the Marten document session with Correlation ID from the current context.
    /// This should be called before saving events to ensure Correlation ID is included in event metadata.
    /// </summary>
    /// <param name="session">The Marten document session to enrich</param>
    void EnrichWithCorrelationId(IDocumentSession session);
}

