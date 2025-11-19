using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using InvestmentHub.Infrastructure.Data;

namespace InvestmentHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HealthController> _logger;
    private readonly ApplicationOptions _options;

    public HealthController(
        ApplicationDbContext context, 
        ILogger<HealthController> logger,
        IOptions<ApplicationOptions> options)
    {
        _context = context;
        _logger = logger;
        _options = options.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        _logger.LogInformation("Health check requested at {Time}", DateTime.UtcNow);
        
        try
        {
            // Check database connectivity
            var canConnect = await _context.Database.CanConnectAsync();
            
            return Ok(new
            {
                Status = "Healthy",
                Application = _options.Name,
                Version = _options.Version,
                Environment = _options.Environment,
                Database = canConnect ? "Connected" : "Disconnected",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new
            {
                Status = "Unhealthy",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
