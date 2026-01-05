namespace InvestmentHub.Domain.Entities;

/// <summary>
/// Represents a financial report in the global library.
/// Links to existing Instrument (stock/bond) for company/asset reference.
/// </summary>
public class FinancialReport
{
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the Instrument (stock/bond) this report is about.
    /// </summary>
    public Guid InstrumentId { get; set; }

    public int Year { get; set; }
    public int? Quarter { get; set; }  // null = annual, 1-4 = quarterly
    public ReportType ReportType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string BlobUrl { get; set; } = string.Empty;
    public ReportStatus Status { get; set; }
    public int ChunkCount { get; set; }
    public Guid UploadedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Navigation property to Instrument.
    /// </summary>
    public Instrument Instrument { get; set; } = null!;

    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}

public enum ReportType
{
    Annual10K,
    Quarterly10Q,
    AnnualReport,
    QuarterlyReport,
    Earnings,
    Other
}

public enum ReportStatus
{
    Processing,
    Ready,
    Failed
}
