using InvestmentHub.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace InvestmentHub.API.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly InstrumentImporter _importer;
    private readonly IWebHostEnvironment _environment;

    public AdminController(InstrumentImporter importer, IWebHostEnvironment environment)
    {
        _importer = importer;
        _environment = environment;
    }

    [HttpPost("sync-instruments")]
    public async Task<IActionResult> SyncInstruments()
    {
        var sourceFile = Path.Combine(AppContext.BaseDirectory, "all_instruments_list.json");
        var destFile = Path.Combine(AppContext.BaseDirectory, "valid_instruments.json");

        if (!System.IO.File.Exists(sourceFile))
        {
            return NotFound($"Source file not found at {sourceFile}");
        }

        try
        {
            var validCount = await _importer.SyncWithYahooAsync(sourceFile, destFile);
            return Ok(new 
            { 
                Message = "Synchronization completed successfully", 
                ValidInstrumentsCount = validCount,
                OutputFile = destFile 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    [HttpPost("sync-global")]
    public async Task<IActionResult> SyncGlobal()
    {
        var sourceFile = Path.Combine(AppContext.BaseDirectory, "import_others.json");
        var destFile = Path.Combine(AppContext.BaseDirectory, "valid_global_instruments.json");

        // The user might put the file in the root, so let's check there too relative to execution content root
        if (!System.IO.File.Exists(sourceFile))
        {
            // Fallback to ContentRootPath (d:\Github\InvestmentHub\src\InvestmentHub.API)
            // or solution root. Let's try to locate it relative to project structure if mostly running locally
             sourceFile = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "..", "import_others.json"));
        }

        if (!System.IO.File.Exists(sourceFile))
        {
            return NotFound($"Source file not found at {sourceFile}");
        }

        try
        {
            var validCount = await _importer.SyncGlobalWithYahooAsync(sourceFile, destFile);
            
            // Optionally auto-import effectively?
            // For now just Sync/Validate as per previous pattern.
            
            return Ok(new 
            { 
                Message = "Global Instruments Synchronization completed successfully", 
                ValidInstrumentsCount = validCount,
                OutputFile = destFile 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    [HttpPost("import-global")]
    public async Task<IActionResult> ImportGlobal()
    {
        var sourceFile = Path.Combine(AppContext.BaseDirectory, "valid_global_instruments.json");
        
        if (!System.IO.File.Exists(sourceFile))
        {
            return NotFound($"Valid instruments file not found at {sourceFile}. Run sync-global first.");
        }

        try
        {
            await _importer.ImportGlobalAsync(sourceFile);
            return Ok(new { Message = "Global instruments imported successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }
}
