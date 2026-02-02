using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioBalance.Data;
using PortfolioBalance.DTOs;
using PortfolioBalance.Models;
using PortfolioBalance.Services;

namespace PortfolioBalance.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly PortfolioDbContext _context;
    private readonly AuthService _authService;

    public AuthController(PortfolioDbContext context, AuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto registerDto)
    {
        // Check if username already exists
        if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
        {
            return BadRequest(new { message = "Username already exists" });
        }

        // Check if email already exists
        if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
        {
            return BadRequest(new { message = "Email already exists" });
        }

        // Create new user
        var user = new User
        {
            Username = registerDto.Username,
            Email = registerDto.Email,
            PasswordHash = _authService.HashPassword(registerDto.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Create default allocations for the new user (20% each)
        var investmentTypes = await _context.InvestmentTypes.ToListAsync();
        foreach (var investmentType in investmentTypes)
        {
            _context.UserInvestmentTypeAllocations.Add(new UserInvestmentTypeAllocation
            {
                UserId = user.Id,
                InvestmentTypeId = investmentType.Id,
                AllocationPercentage = 20.0m
            });
        }
        await _context.SaveChangesAsync();

        // Generate JWT token
        var token = _authService.GenerateJwtToken(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            Username = user.Username,
            Email = user.Email
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
    {
        // Find user by username or email
        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.Username == loginDto.UsernameOrEmail || u.Email == loginDto.UsernameOrEmail);

        if (user == null || !_authService.VerifyPassword(loginDto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid username/email or password" });
        }

        // Generate JWT token
        var token = _authService.GenerateJwtToken(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            Username = user.Username,
            Email = user.Email
        });
    }
}
