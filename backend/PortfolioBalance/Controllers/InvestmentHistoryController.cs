using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioBalance.Data;
using PortfolioBalance.DTOs;
using PortfolioBalance.Models;
using PortfolioBalance.Services;

namespace PortfolioBalance.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvestmentHistoryController : ControllerBase
{
    private readonly PortfolioDbContext _context;
    private readonly AuthService _authService;

    public InvestmentHistoryController(PortfolioDbContext context, AuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    [HttpGet("investment/{investmentId}")]
    public async Task<ActionResult<IEnumerable<InvestmentHistoryDto>>> GetInvestmentHistory(
        int investmentId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        // Verify the investment belongs to the user
        var investment = await _context.Investments
            .FirstOrDefaultAsync(i => i.Id == investmentId && i.UserId == userId.Value);

        if (investment == null)
        {
            return NotFound("Investment not found");
        }

        var query = _context.InvestmentHistories
            .Where(h => h.InvestmentId == investmentId);

        if (startDate.HasValue)
        {
            query = query.Where(h => h.RecordedDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(h => h.RecordedDate <= endDate.Value);
        }

        var history = await query
            .OrderBy(h => h.RecordedDate)
            .Select(h => new InvestmentHistoryDto
            {
                Id = h.Id,
                InvestmentId = h.InvestmentId,
                Value = h.Value,
                UnitValue = h.UnitValue,
                Quantity = h.Quantity,
                RecordedDate = h.RecordedDate,
                Notes = h.Notes,
                InvestmentName = investment.Name
            })
            .ToListAsync();

        return Ok(history);
    }

    [HttpPost]
    public async Task<ActionResult<InvestmentHistoryDto>> CreateHistoryEntry([FromBody] CreateInvestmentHistoryDto dto)
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        // Verify the investment belongs to the user
        var investment = await _context.Investments
            .FirstOrDefaultAsync(i => i.Id == dto.InvestmentId && i.UserId == userId.Value);

        if (investment == null)
        {
            return NotFound("Investment not found");
        }

        var history = new InvestmentHistory
        {
            InvestmentId = dto.InvestmentId,
            Value = dto.Value,
            UnitValue = dto.UnitValue,
            Quantity = dto.Quantity,
            RecordedDate = dto.RecordedDate ?? DateTime.UtcNow,
            Notes = dto.Notes
        };

        _context.InvestmentHistories.Add(history);
        await _context.SaveChangesAsync();

        var result = new InvestmentHistoryDto
        {
            Id = history.Id,
            InvestmentId = history.InvestmentId,
            Value = history.Value,
            UnitValue = history.UnitValue,
            Quantity = history.Quantity,
            RecordedDate = history.RecordedDate,
            Notes = history.Notes,
            InvestmentName = investment.Name
        };

        return CreatedAtAction(nameof(GetInvestmentHistory), new { investmentId = history.InvestmentId }, result);
    }

    [HttpGet("portfolio")]
    public async Task<ActionResult<IEnumerable<PortfolioHistoryDto>>> GetPortfolioHistory(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        // Get all user investments
        var userInvestmentIds = await _context.Investments
            .Where(i => i.UserId == userId.Value)
            .Select(i => i.Id)
            .ToListAsync();

        if (!userInvestmentIds.Any())
        {
            return Ok(new List<PortfolioHistoryDto>());
        }

        var query = _context.InvestmentHistories
            .Include(h => h.Investment)
            .ThenInclude(i => i!.InvestmentType)
            .Where(h => userInvestmentIds.Contains(h.InvestmentId));

        if (startDate.HasValue)
        {
            query = query.Where(h => h.RecordedDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(h => h.RecordedDate <= endDate.Value);
        }

        var histories = await query.ToListAsync();

        // Group by date and aggregate values
        var portfolioHistory = histories
            .GroupBy(h => h.RecordedDate.Date)
            .Select(g => new PortfolioHistoryDto
            {
                Date = g.Key,
                TotalValue = g.Sum(h => h.Value),
                ValueByType = g
                    .GroupBy(h => h.Investment!.InvestmentType!.Name)
                    .ToDictionary(
                        typeGroup => typeGroup.Key,
                        typeGroup => typeGroup.Sum(h => h.Value)
                    )
            })
            .OrderBy(p => p.Date)
            .ToList();

        return Ok(portfolioHistory);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHistoryEntry(int id)
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        var history = await _context.InvestmentHistories
            .Include(h => h.Investment)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (history == null || history.Investment?.UserId != userId.Value)
        {
            return NotFound();
        }

        _context.InvestmentHistories.Remove(history);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
