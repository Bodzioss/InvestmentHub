using InvestmentHub.API.Services;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Infrastructure.Data;
using InvestmentHub.Infrastructure.Identity;
using InvestmentHub.Contracts.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestmentHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly TokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        TokenService tokenService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _context = context;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userExists = await _userManager.FindByEmailAsync(request.Email);
        if (userExists != null)
        {
            return BadRequest("Email is already taken");
        }

        // Create Identity User
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }
            return BadRequest(ModelState);
        }

        // Create Domain User (using the same ID)
        // We use a transaction to ensure both are created or neither
        // Note: In a distributed system this might need an outbox pattern, 
        // but for now we are using the same DB context so it's safe if we save changes.
        // However, UserManager saves changes immediately, so we need to be careful.
        // Ideally we would wrap this in a transaction scope, but for simplicity:
        
        try
        {
            var domainUser = new User(
                new Domain.ValueObjects.UserId(user.Id),
                request.Name,
                request.Email,
                DateTime.UtcNow
            );

            _context.DomainUsers.Add(domainUser);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create domain user for {Email}", request.Email);
            // Cleanup identity user if domain user creation fails
            await _userManager.DeleteAsync(user);
            return StatusCode(500, "Failed to create user profile");
        }

        return Ok(new AuthResponse
        {
            Email = user.Email!,
            Token = _tokenService.CreateToken(user)
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            return Unauthorized("Invalid credentials");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!isPasswordValid)
        {
            return Unauthorized("Invalid credentials");
        }

        var token = _tokenService.CreateToken(user);

        return Ok(new AuthResponse
        {
            Email = user.Email!,
            Token = token
        });
    }
}
