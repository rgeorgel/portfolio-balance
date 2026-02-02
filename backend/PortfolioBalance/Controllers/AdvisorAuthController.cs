using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioBalance.Data;
using PortfolioBalance.DTOs;
using PortfolioBalance.Models;
using PortfolioBalance.Services;

namespace PortfolioBalance.Controllers;

[ApiController]
[Route("api/advisor/auth")]
public class AdvisorAuthController : ControllerBase
{
    private readonly PortfolioDbContext _context;
    private readonly AdvisorAuthService _advisorAuthService;

    public AdvisorAuthController(PortfolioDbContext context, AdvisorAuthService advisorAuthService)
    {
        _context = context;
        _advisorAuthService = advisorAuthService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AdvisorAuthResponseDto>> Register(AdvisorRegisterDto registerDto)
    {
        // Check if username already exists
        if (await _context.Advisors.AnyAsync(a => a.Username == registerDto.Username))
        {
            return BadRequest(new { message = "Username already exists" });
        }

        // Check if email already exists
        if (await _context.Advisors.AnyAsync(a => a.Email == registerDto.Email))
        {
            return BadRequest(new { message = "Email already exists" });
        }

        // Create new advisor
        var advisor = new Advisor
        {
            Username = registerDto.Username,
            Email = registerDto.Email,
            PasswordHash = _advisorAuthService.HashPassword(registerDto.Password),
            FullName = registerDto.FullName
        };

        _context.Advisors.Add(advisor);
        await _context.SaveChangesAsync();

        // Generate JWT token
        var token = _advisorAuthService.GenerateJwtToken(advisor);

        return Ok(new AdvisorAuthResponseDto
        {
            Token = token,
            Username = advisor.Username,
            Email = advisor.Email,
            FullName = advisor.FullName,
            IsAdvisor = true
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AdvisorAuthResponseDto>> Login(AdvisorLoginDto loginDto)
    {
        // Find advisor by username or email
        var advisor = await _context.Advisors.FirstOrDefaultAsync(a =>
            a.Username == loginDto.UsernameOrEmail || a.Email == loginDto.UsernameOrEmail);

        if (advisor == null || !_advisorAuthService.VerifyPassword(loginDto.Password, advisor.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid username/email or password" });
        }

        if (!advisor.IsActive)
        {
            return Unauthorized(new { message = "Advisor account is disabled" });
        }

        // Generate JWT token
        var token = _advisorAuthService.GenerateJwtToken(advisor);

        return Ok(new AdvisorAuthResponseDto
        {
            Token = token,
            Username = advisor.Username,
            Email = advisor.Email,
            FullName = advisor.FullName,
            IsAdvisor = true
        });
    }
}
