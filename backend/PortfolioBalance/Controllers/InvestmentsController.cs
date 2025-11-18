using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioBalance.Data;
using PortfolioBalance.DTOs;
using PortfolioBalance.Models;

namespace PortfolioBalance.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvestmentsController : ControllerBase
{
    private readonly PortfolioDbContext _context;

    public InvestmentsController(PortfolioDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InvestmentDto>>> GetInvestments([FromQuery] int? investmentTypeId = null)
    {
        var query = _context.Investments.AsQueryable();

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
                Weight = i.Weight,
                CreatedDate = i.CreatedDate
            })
            .ToListAsync();

        return Ok(investments);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InvestmentDto>> GetInvestment(int id)
    {
        var investment = await _context.Investments
            .Where(i => i.Id == id)
            .Select(i => new InvestmentDto
            {
                Id = i.Id,
                InvestmentTypeId = i.InvestmentTypeId,
                Name = i.Name,
                CurrentValue = i.CurrentValue,
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
        var investmentType = await _context.InvestmentTypes.FindAsync(dto.InvestmentTypeId);

        if (investmentType == null)
        {
            return BadRequest("Invalid investment type");
        }

        var investment = new Investment
        {
            InvestmentTypeId = dto.InvestmentTypeId,
            Name = dto.Name,
            CurrentValue = dto.CurrentValue,
            Weight = dto.Weight,
            CreatedDate = DateTime.UtcNow
        };

        _context.Investments.Add(investment);
        await _context.SaveChangesAsync();

        var result = new InvestmentDto
        {
            Id = investment.Id,
            InvestmentTypeId = investment.InvestmentTypeId,
            Name = investment.Name,
            CurrentValue = investment.CurrentValue,
            Weight = investment.Weight,
            CreatedDate = investment.CreatedDate
        };

        return CreatedAtAction(nameof(GetInvestment), new { id = investment.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateInvestment(int id, [FromBody] UpdateInvestmentDto dto)
    {
        var investment = await _context.Investments.FindAsync(id);

        if (investment == null)
        {
            return NotFound();
        }

        investment.Name = dto.Name;
        investment.CurrentValue = dto.CurrentValue;
        investment.Weight = dto.Weight;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteInvestment(int id)
    {
        var investment = await _context.Investments.FindAsync(id);

        if (investment == null)
        {
            return NotFound();
        }

        _context.Investments.Remove(investment);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
