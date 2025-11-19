using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioBalance.DTOs;
using PortfolioBalance.Services;
using System.Security.Claims;

namespace PortfolioBalance.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfitabilityController : ControllerBase
{
    private readonly ProfitabilityService _profitabilityService;

    public ProfitabilityController(ProfitabilityService profitabilityService)
    {
        _profitabilityService = profitabilityService;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }

    /// <summary>
    /// Gets overall portfolio profitability metrics
    /// </summary>
    /// <param name="startDate">Optional start date for filtering (ISO 8601 format)</param>
    /// <param name="endDate">Optional end date for filtering (ISO 8601 format)</param>
    [HttpGet("portfolio")]
    public async Task<ActionResult<PortfolioProfitabilityDto>> GetPortfolioProfitability(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var userId = GetUserId();
        var profitability = await _profitabilityService.GetPortfolioProfitabilityAsync(userId, startDate, endDate);
        return Ok(profitability);
    }

    /// <summary>
    /// Gets profitability metrics for a specific investment
    /// </summary>
    /// <param name="id">Investment ID</param>
    [HttpGet("investment/{id}")]
    public async Task<ActionResult<InvestmentProfitabilityDto>> GetInvestmentProfitability(int id)
    {
        var userId = GetUserId();
        var profitability = await _profitabilityService.GetInvestmentProfitabilityAsync(userId, id);

        if (profitability == null)
        {
            return NotFound(new { message = "Investment not found" });
        }

        return Ok(profitability);
    }

    /// <summary>
    /// Gets profitability metrics grouped by investment type
    /// </summary>
    [HttpGet("by-type")]
    public async Task<ActionResult<List<InvestmentTypeProfitabilityDto>>> GetProfitabilityByType()
    {
        var userId = GetUserId();
        var profitability = await _profitabilityService.GetProfitabilityByTypeAsync(userId);
        return Ok(profitability);
    }

    /// <summary>
    /// Gets top and bottom performing investments
    /// </summary>
    /// <param name="limit">Number of top/bottom performers to return (default: 5)</param>
    [HttpGet("top-performers")]
    public async Task<ActionResult<TopPerformersDto>> GetTopPerformers([FromQuery] int limit = 5)
    {
        var userId = GetUserId();

        if (limit < 1 || limit > 20)
        {
            return BadRequest(new { message = "Limit must be between 1 and 20" });
        }

        var performers = await _profitabilityService.GetTopPerformersAsync(userId, limit);
        return Ok(performers);
    }
}
