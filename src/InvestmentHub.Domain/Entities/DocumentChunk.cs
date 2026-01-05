using Pgvector;

namespace InvestmentHub.Domain.Entities;

/// <summary>
/// Represents a chunk of text from a financial report with its vector embedding.
/// </summary>
public class DocumentChunk
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public Vector Embedding { get; set; } = null!;
    public int? PageNumber { get; set; }
    public DateTime CreatedAt { get; set; }

    public FinancialReport Report { get; set; } = null!;
}
