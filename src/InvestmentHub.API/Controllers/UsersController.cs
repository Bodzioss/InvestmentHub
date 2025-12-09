using MediatR;
using Microsoft.AspNetCore.Mvc;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Repositories;
using AutoMapper;
using InvestmentHub.Contracts;
using InvestmentHub.Infrastructure.Data;

namespace InvestmentHub.API.Controllers;

/// <summary>
/// Controller for managing users.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UsersController> _logger;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the UsersController class.
    /// </summary>
    /// <param name="userRepository">The user repository</param>
    /// <param name="context">The database context</param>
    /// <param name="logger">The logger</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public UsersController(IUserRepository userRepository, ApplicationDbContext context, ILogger<UsersController> logger, IMapper mapper)
    {
        _userRepository = userRepository;
        _context = context;
        _logger = logger;
        _mapper = mapper;
    }

    /// <summary>
    /// Gets all users.
    /// </summary>
    /// <returns>The users data</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = await _userRepository.GetAllAsync();
            var response = users.Select(u => _mapper.Map<UserResponseDto>(u)).ToList();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all users");
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>The user data</returns>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser([FromRoute] string userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(UserId.FromString(userId));
            
            if (user == null)
            {
                return NotFound(new { Error = "User not found" });
            }

            var response = _mapper.Map<UserResponseDto>(user);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid user ID format: {UserId}", userId);
            return BadRequest(new { Error = "Invalid user ID format" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", userId);
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a user by email.
    /// </summary>
    /// <param name="email">The user email</param>
    /// <returns>The user data</returns>
    [HttpGet("by-email/{email}")]
    public async Task<IActionResult> GetUserByEmail([FromRoute] string email)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(email);
            
            if (user == null)
            {
                return NotFound(new { Error = "User not found" });
            }

            var response = _mapper.Map<UserResponseDto>(user);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email {Email}", email);
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }
    /// <summary>
    /// Updates a user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="request">The update request</param>
    /// <returns>No content</returns>
    [HttpPut("{userId}")]
    public async Task<IActionResult> UpdateUser([FromRoute] string userId, [FromBody] InvestmentHub.Contracts.Users.UpdateUserRequest request)
    {
        // Note: Ideally this should go through MediatR command, but for simplicity we'll update directly via repository/context
        // However, since we have separate Identity and Domain users, we need to be careful.
        // For now, we only update the Domain User name.
        
        try
        {
            var id = UserId.FromString(userId);
            var user = await _userRepository.GetByIdAsync(id);
            
            if (user == null)
            {
                return NotFound(new { Error = "User not found" });
            }

            // Update domain user
            user.UpdateName(request.Name);
            
            // We need to attach the user to context to track changes if it was retrieved via repository that doesn't track
            // But since we injected context, let's use it to save.
            // Assuming repository uses the same context instance (scoped).
            
            await _context.SaveChangesAsync();
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }
}

