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
public class InvestmentsController : ControllerBase
{
    private readonly PortfolioDbContext _context;
    private readonly AuthService _authService;

    public InvestmentsController(PortfolioDbContext context, AuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InvestmentDto>>> GetInvestments([FromQuery] int? investmentTypeId = null)
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        var query = _context.Investments.Where(i => i.UserId == userId.Value);

        if (investmentTypeId.HasValue)
        {
            query = query.Where(i => i.InvestmentTypeId == investmentTypeId.Value);
        }

        var investments = await query
            .Select(i => new InvestmentDto
            {
                Id = i.Id,
                InvestmentTypeId = i.InvestmentTypeId,
                Name = i.Name,
                CurrentValue = i.CurrentValue,
                UnitValue = i.UnitValue,
                Quantity = i.Quantity,
                Weight = i.Weight,
                CreatedDate = i.CreatedDate
            })
            .ToListAsync();

        return Ok(investments);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InvestmentDto>> GetInvestment(int id)
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        var investment = await _context.Investments
            .Where(i => i.Id == id && i.UserId == userId.Value)
            .Select(i => new InvestmentDto
            {
                Id = i.Id,
                InvestmentTypeId = i.InvestmentTypeId,
                Name = i.Name,
                CurrentValue = i.CurrentValue,
                UnitValue = i.UnitValue,
                Quantity = i.Quantity,
                Weight = i.Weight,
                CreatedDate = i.CreatedDate
            })
            .FirstOrDefaultAsync();

        if (investment == null)
        {
            return NotFound();
        }

        return Ok(investment);
    }

    [HttpPost]
    public async Task<ActionResult<InvestmentDto>> CreateInvestment([FromBody] CreateInvestmentDto dto)
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        var investmentType = await _context.InvestmentTypes.FindAsync(dto.InvestmentTypeId);

        if (investmentType == null)
        {
            return BadRequest("Invalid investment type");
        }

        var investment = new Investment
        {
            UserId = userId.Value,
            InvestmentTypeId = dto.InvestmentTypeId,
            Name = dto.Name,
            CurrentValue = dto.CurrentValue,
            UnitValue = dto.UnitValue,
            Quantity = dto.Quantity,
            Weight = dto.Weight,
            CreatedDate = DateTime.UtcNow
        };

        _context.Investments.Add(investment);
        await _context.SaveChangesAsync();

        // Record initial history entry
        var history = new InvestmentHistory
        {
            InvestmentId = investment.Id,
            Value = investment.CurrentValue,
            UnitValue = investment.UnitValue,
            Quantity = investment.Quantity,
            RecordedDate = DateTime.UtcNow,
            Notes = "Initial investment"
        };
        _context.InvestmentHistories.Add(history);
        await _context.SaveChangesAsync();

        var result = new InvestmentDto
        {
            Id = investment.Id,
            InvestmentTypeId = investment.InvestmentTypeId,
            Name = investment.Name,
            CurrentValue = investment.CurrentValue,
            UnitValue = investment.UnitValue,
            Quantity = investment.Quantity,
            Weight = investment.Weight,
            CreatedDate = investment.CreatedDate
        };

        return CreatedAtAction(nameof(GetInvestment), new { id = investment.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateInvestment(int id, [FromBody] UpdateInvestmentDto dto)
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        var investment = await _context.Investments.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId.Value);

        if (investment == null)
        {
            return NotFound();
        }

        // Check if value changed to record history
        bool valueChanged = investment.CurrentValue != dto.CurrentValue ||
                           investment.UnitValue != dto.UnitValue ||
                           investment.Quantity != dto.Quantity;

        investment.Name = dto.Name;
        investment.CurrentValue = dto.CurrentValue;
        investment.UnitValue = dto.UnitValue;
        investment.Quantity = dto.Quantity;
        investment.Weight = dto.Weight;

        await _context.SaveChangesAsync();

        // Record history entry if value changed
        if (valueChanged)
        {
            var history = new InvestmentHistory
            {
                InvestmentId = investment.Id,
                Value = investment.CurrentValue,
                UnitValue = investment.UnitValue,
                Quantity = investment.Quantity,
                RecordedDate = DateTime.UtcNow,
                Notes = "Value updated"
            };
            _context.InvestmentHistories.Add(history);
            await _context.SaveChangesAsync();
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteInvestment(int id)
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        var investment = await _context.Investments.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId.Value);

        if (investment == null)
        {
            return NotFound();
        }

        _context.Investments.Remove(investment);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
