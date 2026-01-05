using System.Text;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Pgvector;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace InvestmentHub.Infrastructure.AI;

/// <summary>
/// Processes PDF documents: extracts text, chunks it, and generates embeddings.
/// </summary>
public class DocumentProcessor
{
    private readonly IGeminiService _gemini;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DocumentProcessor> _logger;

    private const int MaxChunkSize = 2000;  // ~500 tokens
    private const int ChunkOverlap = 200;   // Overlap between chunks

    public DocumentProcessor(
        IGeminiService gemini,
        ApplicationDbContext db,
        ILogger<DocumentProcessor> logger)
    {
        _gemini = gemini;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Processes a PDF file and stores chunks with embeddings.
    /// </summary>
    public async Task ProcessPdfAsync(Guid reportId, Stream pdfStream, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting PDF processing for report {ReportId}", reportId);

        try
        {
            // 1. Extract text from PDF
            var (fullText, pageTexts) = ExtractTextFromPdf(pdfStream);
            _logger.LogInformation("Extracted {Length} characters from PDF", fullText.Length);

            if (string.IsNullOrWhiteSpace(fullText))
            {
                _logger.LogWarning("No text extracted from PDF for report {ReportId}", reportId);
                await UpdateReportStatus(reportId, ReportStatus.Failed, 0, ct);
                return;
            }

            // 2. Chunk text
            var chunks = ChunkText(fullText, MaxChunkSize, ChunkOverlap);
            _logger.LogInformation("Created {Count} chunks", chunks.Count);

            // 3. Generate embeddings and save
            var chunkIndex = 0;
            foreach (var chunk in chunks)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    var embedding = await _gemini.GetEmbeddingAsync(chunk, ct);

                    if (embedding.Length > 0)
                    {
                        var documentChunk = new DocumentChunk
                        {
                            Id = Guid.NewGuid(),
                            ReportId = reportId,
                            ChunkIndex = chunkIndex,
                            Content = chunk,
                            Embedding = new Vector(embedding),
                            PageNumber = FindPageForChunk(chunk, pageTexts),
                            CreatedAt = DateTime.UtcNow
                        };

                        _db.DocumentChunks.Add(documentChunk);
                        chunkIndex++;

                        // Rate limiting - Gemini has 15 RPM for free tier
                        if (chunkIndex % 10 == 0)
                        {
                            await _db.SaveChangesAsync(ct);
                            await Task.Delay(1000, ct); // Pause to avoid rate limit
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process chunk {Index}", chunkIndex);
                }
            }

            await _db.SaveChangesAsync(ct);
            await UpdateReportStatus(reportId, ReportStatus.Ready, chunkIndex, ct);

            _logger.LogInformation("Completed processing report {ReportId} with {Count} chunks", reportId, chunkIndex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process PDF for report {ReportId}", reportId);
            await UpdateReportStatus(reportId, ReportStatus.Failed, 0, ct);
            throw;
        }
    }

    private (string fullText, Dictionary<int, string> pageTexts) ExtractTextFromPdf(Stream pdfStream)
    {
        var fullText = new StringBuilder();
        var pageTexts = new Dictionary<int, string>();

        using var document = PdfDocument.Open(pdfStream);

        foreach (var page in document.GetPages())
        {
            var pageText = page.Text;
            pageTexts[page.Number] = pageText;
            fullText.AppendLine(pageText);
        }

        return (fullText.ToString(), pageTexts);
    }

    private List<string> ChunkText(string text, int maxSize, int overlap)
    {
        var chunks = new List<string>();

        // Split into sentences first
        var sentences = text.Split(new[] { ". ", ".\n", "?\n", "!\n" }, StringSplitOptions.RemoveEmptyEntries);

        var currentChunk = new StringBuilder();
        var previousSentences = new Queue<string>();

        foreach (var sentence in sentences)
        {
            var trimmedSentence = sentence.Trim();
            if (string.IsNullOrWhiteSpace(trimmedSentence)) continue;

            // If adding this sentence would exceed max size, save current chunk
            if (currentChunk.Length + trimmedSentence.Length > maxSize && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());

                // Start new chunk with overlap from previous sentences
                currentChunk.Clear();
                foreach (var prev in previousSentences)
                {
                    currentChunk.Append(prev).Append(". ");
                }
            }

            currentChunk.Append(trimmedSentence).Append(". ");

            // Keep track of last few sentences for overlap
            previousSentences.Enqueue(trimmedSentence);
            if (previousSentences.Count > 3)
            {
                previousSentences.Dequeue();
            }
        }

        // Don't forget the last chunk
        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }

    private int? FindPageForChunk(string chunk, Dictionary<int, string> pageTexts)
    {
        // Find which page contains the start of this chunk
        var searchText = chunk.Length > 100 ? chunk.Substring(0, 100) : chunk;

        foreach (var (pageNum, pageText) in pageTexts)
        {
            if (pageText.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            {
                return pageNum;
            }
        }

        return null;
    }

    private async Task UpdateReportStatus(Guid reportId, ReportStatus status, int chunkCount, CancellationToken ct)
    {
        var report = await _db.FinancialReports.FindAsync(new object[] { reportId }, ct);
        if (report != null)
        {
            report.Status = status;
            report.ChunkCount = chunkCount;
            await _db.SaveChangesAsync(ct);
        }
    }
}
