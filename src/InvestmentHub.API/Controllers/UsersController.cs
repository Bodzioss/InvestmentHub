using MediatR;
using Microsoft.AspNetCore.Mvc;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Repositories;
using AutoMapper;
using InvestmentHub.Contracts;

namespace InvestmentHub.API.Controllers;

/// <summary>
/// Controller for managing users.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UsersController> _logger;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the UsersController class.
    /// </summary>
    /// <param name="userRepository">The user repository</param>
    /// <param name="logger">The logger</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public UsersController(IUserRepository userRepository, ILogger<UsersController> _logger, IMapper mapper)
    {
        _userRepository = userRepository;
        this._logger = _logger;
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
}

