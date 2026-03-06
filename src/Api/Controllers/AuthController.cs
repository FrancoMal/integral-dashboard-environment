using System.Security.Claims;
using Api.Data;
using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly AppDbContext _db;

    public AuthController(AuthService authService, AppDbContext db)
    {
        _authService = authService;
        _db = db;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.Login(request.Username, request.Password);
            if (result is null)
                return Unauthorized(new { message = "Invalid username or password" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred during login", error = ex.Message });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _authService.Register(request.Username, request.Email, request.Password);
            if (result is null)
                return Conflict(new { message = "Username or email already exists" });

            return Created("", result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred during registration", error = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        try
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            var user = await _db.Users.FindAsync(userId.Value);
            if (user is null)
                return NotFound(new { message = "User not found" });

            return Ok(new UserDto(
                Id: user.Id,
                Username: user.Username,
                Email: user.Email,
                Role: user.Role,
                CreatedAt: user.CreatedAt,
                IsActive: user.IsActive
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        }
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;
    }
}
