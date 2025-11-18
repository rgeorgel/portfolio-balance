using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioBalance.Data;
using PortfolioBalance.DTOs;

namespace PortfolioBalance.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvestmentTypesController : ControllerBase
{
    private readonly PortfolioDbContext _context;

    public InvestmentTypesController(PortfolioDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InvestmentTypeDto>>> GetInvestmentTypes()
    {
        var types = await _context.InvestmentTypes
            .Include(t => t.Investments)
            .Select(t => new InvestmentTypeDto
            {
                Id = t.Id,
                Name = t.Name,
                AllocationPercentage = t.AllocationPercentage,
                CurrentTotalValue = t.Investments.Sum(i => i.CurrentValue)
            })
            .ToListAsync();

        return Ok(types);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InvestmentTypeDto>> GetInvestmentType(int id)
    {
        var type = await _context.InvestmentTypes
            .Include(t => t.Investments)
            .Where(t => t.Id == id)
            .Select(t => new InvestmentTypeDto
            {
                Id = t.Id,
                Name = t.Name,
                AllocationPercentage = t.AllocationPercentage,
                CurrentTotalValue = t.Investments.Sum(i => i.CurrentValue)
            })
            .FirstOrDefaultAsync();

        if (type == null)
        {
            return NotFound();
        }

        return Ok(type);
    }

    [HttpPut("{id}/allocation")]
    public async Task<IActionResult> UpdateAllocation(int id, [FromBody] UpdateAllocationDto dto)
    {
        var type = await _context.InvestmentTypes.FindAsync(id);

        if (type == null)
        {
            return NotFound();
        }

        type.AllocationPercentage = dto.AllocationPercentage;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("allocations")]
    public async Task<IActionResult> UpdateAllocations([FromBody] List<UpdateAllocationDto> allocations)
    {
        var total = allocations.Sum(a => a.AllocationPercentage);

        if (Math.Abs(total - 100) > 0.01m)
        {
            return BadRequest("Total allocation percentage must equal 100%");
        }

        foreach (var allocation in allocations)
        {
            var type = await _context.InvestmentTypes.FindAsync(allocation.Id);
            if (type != null)
            {
                type.AllocationPercentage = allocation.AllocationPercentage;
            }
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
