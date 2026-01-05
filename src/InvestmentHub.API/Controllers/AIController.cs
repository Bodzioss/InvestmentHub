using InvestmentHub.Domain.Entities;
using InvestmentHub.Infrastructure.AI;
using InvestmentHub.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.API.Controllers;

/// <summary>
/// Controller for AI-powered financial document analysis.
/// </summary>
[ApiController]
[Route("api/ai")]
public class AIController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IGeminiService _gemini;
    private readonly DocumentProcessor _documentProcessor;
    private readonly VectorSearchService _vectorSearch;
    private readonly ILogger<AIController> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public AIController(
        ApplicationDbContext db,
        IGeminiService gemini,
        DocumentProcessor documentProcessor,
        VectorSearchService vectorSearch,
        ILogger<AIController> logger,
        IServiceScopeFactory scopeFactory)
    {
        _db = db;
        _gemini = gemini;
        _documentProcessor = documentProcessor;
        _vectorSearch = vectorSearch;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Gets instruments for report upload dropdown.
    /// Supports optional search query for typeahead functionality.
    /// </summary>
    [HttpGet("instruments")]
    public async Task<IActionResult> GetInstruments([FromQuery] string? search, [FromQuery] int limit = 20)
    {
        var query = _db.Instruments.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(i =>
                i.Symbol.Ticker.ToLower().Contains(searchLower) ||
                i.Name.ToLower().Contains(searchLower));
        }

        var instruments = await query
            .OrderBy(i => i.Symbol.Ticker)
            .Take(limit)
            .Select(i => new
            {
                id = i.Id,
                ticker = i.Symbol.Ticker,
                name = i.Name,
                exchange = i.Symbol.Exchange
            })
            .ToListAsync();

        return Ok(instruments);
    }

    /// <summary>
    /// Gets all financial reports in the library.
    /// </summary>
    [HttpGet("reports")]
    public async Task<IActionResult> GetReports([FromQuery] Guid? instrumentId, [FromQuery] int? year)
    {
        var query = _db.FinancialReports
            .Include(r => r.Instrument)
            .AsQueryable();

        if (instrumentId.HasValue)
        {
            query = query.Where(r => r.InstrumentId == instrumentId.Value);
        }

        if (year.HasValue)
        {
            query = query.Where(r => r.Year == year.Value);
        }

        var reports = await query
            .OrderByDescending(r => r.Year)
            .ThenBy(r => r.Quarter)
            .Select(r => new
            {
                id = r.Id,
                instrumentId = r.InstrumentId,
                ticker = r.Instrument.Symbol.Ticker,
                instrumentName = r.Instrument.Name,
                year = r.Year,
                quarter = r.Quarter,
                reportType = r.ReportType.ToString(),
                fileName = r.FileName,
                status = r.Status.ToString(),
                chunkCount = r.ChunkCount,
                createdAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(reports);
    }

    /// <summary>
    /// Checks if a report already exists for the given instrument/year/quarter/type.
    /// </summary>
    [HttpGet("reports/exists")]
    public async Task<IActionResult> ReportExists(
        [FromQuery] Guid instrumentId,
        [FromQuery] int year,
        [FromQuery] int? quarter,
        [FromQuery] string reportType)
    {
        var exists = await _db.FinancialReports.AnyAsync(r =>
            r.InstrumentId == instrumentId &&
            r.Year == year &&
            r.Quarter == quarter &&
            r.ReportType.ToString() == reportType);

        return Ok(new { exists });
    }

    /// <summary>
    /// Uploads a new financial report PDF.
    /// </summary>
    [HttpPost("reports/upload")]
    public async Task<IActionResult> UploadReport(
        IFormFile file,
        [FromQuery] Guid instrumentId,
        [FromQuery] int year,
        [FromQuery] int? quarter,
        [FromQuery] string reportType,
        [FromQuery] string userId = "demo-user")
    {
        try
        {
            _logger.LogInformation("Upload request: instrumentId={InstrumentId}, year={Year}, quarter={Quarter}, reportType={ReportType}, userId={UserId}, file={File}",
                instrumentId, year, quarter, reportType, userId, file?.FileName);

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file provided" });
            }

            if (file.ContentType != "application/pdf")
            {
                _logger.LogWarning("Invalid content type: {ContentType}", file.ContentType);
                return BadRequest(new { error = $"Only PDF files are supported. Got: {file.ContentType}" });
            }

            // Check if instrument exists
            var instrument = await _db.Instruments.FindAsync(instrumentId);
            if (instrument == null)
            {
                _logger.LogWarning("Instrument not found: {InstrumentId}", instrumentId);
                return BadRequest(new { error = "Instrument not found" });
            }

            // Check if report already exists
            if (!Enum.TryParse<ReportType>(reportType, out var parsedReportType))
            {
                _logger.LogWarning("Invalid report type: {ReportType}", reportType);
                return BadRequest(new { error = "Invalid report type" });
            }

            var existing = await _db.FinancialReports.FirstOrDefaultAsync(r =>
                r.InstrumentId == instrumentId &&
                r.Year == year &&
                r.Quarter == quarter &&
                r.ReportType == parsedReportType);

            if (existing != null)
            {
                return Ok(new { message = "Report already exists", reportId = existing.Id });
            }

            // Create report record
            var userGuid = userId != null && Guid.TryParse(userId, out var parsed)
                ? parsed
                : Guid.Parse("00000000-0000-0000-0000-000000000001"); // Demo user fallback

            var report = new FinancialReport
            {
                Id = Guid.NewGuid(),
                InstrumentId = instrumentId,
                Year = year,
                Quarter = quarter,
                ReportType = parsedReportType,
                FileName = file.FileName,
                FileSize = file.Length,
                BlobUrl = $"local://{file.FileName}",
                Status = ReportStatus.Processing,
                ChunkCount = 0,
                UploadedByUserId = userGuid,
                CreatedAt = DateTime.UtcNow
            };

            _db.FinancialReports.Add(report);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Created report {ReportId} for instrument {InstrumentId}", report.Id, instrumentId);

            // Copy file to memory BEFORE returning (file stream will be disposed after response)
            var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            var reportId = report.Id;

            // Process PDF in background using new scope (original DbContext will be disposed)
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<DocumentProcessor>();
                    await processor.ProcessPdfAsync(reportId, memoryStream);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process report {ReportId}", reportId);
                }
                finally
                {
                    memoryStream.Dispose();
                }
            });

            return Ok(new
            {
                message = "Report uploaded and processing started",
                reportId = report.Id,
                status = "Processing"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed with exception");
            return StatusCode(500, new { error = ex.Message, details = ex.InnerException?.Message });
        }
    }

    /// <summary>
    /// Chat with AI about financial reports using RAG.
    /// </summary>
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Message is required" });
        }

        try
        {
            // 1. Search for relevant document chunks
            var searchResults = await _vectorSearch.SearchAsync(
                request.Message,
                request.ReportIds,
                topK: 5);

            if (!searchResults.Any())
            {
                return Ok(new ChatResponse
                {
                    Response = "I don't have any financial reports to analyze. Please upload some reports first.",
                    Sources = new List<SourceReference>()
                });
            }

            // 2. Build context from search results
            var context = _vectorSearch.BuildContext(searchResults);

            // 3. Generate response using Gemini
            var response = await _gemini.GenerateResponseAsync(request.Message, context);

            // 4. Build source references
            var sources = searchResults
                .GroupBy(r => r.ReportId)
                .Select(g => new SourceReference
                {
                    ReportId = g.Key,
                    Ticker = g.First().InstrumentTicker,
                    Name = g.First().InstrumentName,
                    Year = g.First().ReportYear,
                    Quarter = g.First().ReportQuarter,
                    ReportType = g.First().ReportType,
                    Pages = g.Where(x => x.PageNumber.HasValue).Select(x => x.PageNumber!.Value).Distinct().ToList()
                })
                .ToList();

            return Ok(new ChatResponse
            {
                Response = response,
                Sources = sources
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat request failed");
            return StatusCode(500, new { error = "Failed to process chat request" });
        }
    }

    /// <summary>
    /// Gets stats about the AI report library.
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalReports = await _db.FinancialReports.CountAsync();
        var readyReports = await _db.FinancialReports.CountAsync(r => r.Status == ReportStatus.Ready);
        var totalChunks = await _db.DocumentChunks.CountAsync();
        var uniqueInstruments = await _db.FinancialReports.Select(r => r.InstrumentId).Distinct().CountAsync();

        return Ok(new
        {
            totalReports,
            readyReports,
            processingReports = totalReports - readyReports,
            totalChunks,
            uniqueInstruments
        });
    }
}

public record ChatRequest(string Message, List<Guid>? ReportIds);

public class ChatResponse
{
    public string Response { get; set; } = string.Empty;
    public List<SourceReference> Sources { get; set; } = new();
}

public class SourceReference
{
    public Guid ReportId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public int? Quarter { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public List<int> Pages { get; set; } = new();
}
