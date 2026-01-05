using InvestmentHub.Domain.Entities;
using InvestmentHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace InvestmentHub.Infrastructure.AI;

/// <summary>
/// Service for vector similarity search using pgvector.
/// </summary>
public class VectorSearchService
{
    private readonly ApplicationDbContext _db;
    private readonly IGeminiService _gemini;
    private readonly ILogger<VectorSearchService> _logger;

    public VectorSearchService(
        ApplicationDbContext db,
        IGeminiService gemini,
        ILogger<VectorSearchService> logger)
    {
        _db = db;
        _gemini = gemini;
        _logger = logger;
    }

    /// <summary>
    /// Searches for relevant document chunks using vector similarity.
    /// </summary>
    /// <param name="query">The search query text</param>
    /// <param name="reportIds">Optional filter by specific report IDs</param>
    /// <param name="topK">Number of results to return</param>
    public async Task<List<SearchResult>> SearchAsync(
        string query,
        List<Guid>? reportIds = null,
        int topK = 5,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Searching for: {Query}", query);

        try
        {
            // 1. Generate embedding for query
            var queryEmbedding = await _gemini.GetEmbeddingAsync(query, ct);
            if (queryEmbedding.Length == 0)
            {
                _logger.LogWarning("Failed to generate embedding for query");
                return new List<SearchResult>();
            }

            var vector = new Vector(queryEmbedding);

            // 2. Build query
            var chunksQuery = _db.DocumentChunks
                .Include(c => c.Report)
                .ThenInclude(r => r.Instrument)
                .AsQueryable();

            // Filter by report IDs if specified
            if (reportIds != null && reportIds.Any())
            {
                chunksQuery = chunksQuery.Where(c => reportIds.Contains(c.ReportId));
            }

            // Only search in ready reports
            chunksQuery = chunksQuery.Where(c => c.Report.Status == ReportStatus.Ready);

            // 3. Perform vector similarity search
            var results = await chunksQuery
                .OrderBy(c => c.Embedding.CosineDistance(vector))
                .Take(topK)
                .Select(c => new SearchResult
                {
                    ChunkId = c.Id,
                    ReportId = c.ReportId,
                    Content = c.Content,
                    PageNumber = c.PageNumber,
                    InstrumentTicker = c.Report.Instrument.Symbol.Ticker,
                    InstrumentName = c.Report.Instrument.Name,
                    ReportYear = c.Report.Year,
                    ReportQuarter = c.Report.Quarter,
                    ReportType = c.Report.ReportType.ToString()
                })
                .ToListAsync(ct);

            _logger.LogInformation("Found {Count} results", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vector search failed");
            return new List<SearchResult>();
        }
    }

    /// <summary>
    /// Builds context string from search results for RAG.
    /// </summary>
    public string BuildContext(List<SearchResult> results)
    {
        if (!results.Any())
        {
            return "No relevant documents found.";
        }

        var context = new System.Text.StringBuilder();

        foreach (var result in results)
        {
            context.AppendLine($"--- From {result.InstrumentTicker} {result.ReportYear} {result.ReportType} (Page {result.PageNumber ?? 0}) ---");
            context.AppendLine(result.Content);
            context.AppendLine();
        }

        return context.ToString();
    }
}

/// <summary>
/// Represents a search result from vector search.
/// </summary>
public class SearchResult
{
    public Guid ChunkId { get; set; }
    public Guid ReportId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
    public string InstrumentTicker { get; set; } = string.Empty;
    public string InstrumentName { get; set; } = string.Empty;
    public int ReportYear { get; set; }
    public int? ReportQuarter { get; set; }
    public string ReportType { get; set; } = string.Empty;
}
