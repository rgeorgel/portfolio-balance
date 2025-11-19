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
public class InvestmentTypesController : ControllerBase
{
    private readonly PortfolioDbContext _context;
    private readonly AuthService _authService;

    public InvestmentTypesController(PortfolioDbContext context, AuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InvestmentTypeDto>>> GetInvestmentTypes()
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        var types = await _context.InvestmentTypes
            .Select(t => new InvestmentTypeDto
            {
                Id = t.Id,
                Name = t.Name,
                AllocationPercentage = t.UserAllocations
                    .Where(ua => ua.UserId == userId.Value)
                    .Select(ua => ua.AllocationPercentage)
                    .FirstOrDefault(),
                CurrentTotalValue = t.Investments
                    .Where(i => i.UserId == userId.Value)
                    .Sum(i => i.CurrentValue)
            })
            .ToListAsync();

        return Ok(types);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InvestmentTypeDto>> GetInvestmentType(int id)
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        var type = await _context.InvestmentTypes
            .Where(t => t.Id == id)
            .Select(t => new InvestmentTypeDto
            {
                Id = t.Id,
                Name = t.Name,
                AllocationPercentage = t.UserAllocations
                    .Where(ua => ua.UserId == userId.Value)
                    .Select(ua => ua.AllocationPercentage)
                    .FirstOrDefault(),
                CurrentTotalValue = t.Investments
                    .Where(i => i.UserId == userId.Value)
                    .Sum(i => i.CurrentValue)
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
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        var type = await _context.InvestmentTypes.FindAsync(id);
        if (type == null)
        {
            return NotFound();
        }

        var allocation = await _context.UserInvestmentTypeAllocations
            .FirstOrDefaultAsync(ua => ua.UserId == userId.Value && ua.InvestmentTypeId == id);

        if (allocation == null)
        {
            // Create new allocation if it doesn't exist
            allocation = new UserInvestmentTypeAllocation
            {
                UserId = userId.Value,
                InvestmentTypeId = id,
                AllocationPercentage = dto.AllocationPercentage
            };
            _context.UserInvestmentTypeAllocations.Add(allocation);
        }
        else
        {
            allocation.AllocationPercentage = dto.AllocationPercentage;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("allocations")]
    public async Task<IActionResult> UpdateAllocations([FromBody] List<UpdateAllocationDto> allocations)
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        var total = allocations.Sum(a => a.AllocationPercentage);

        if (Math.Abs(total - 100) > 0.01m)
        {
            return BadRequest("Total allocation percentage must equal 100%");
        }

        foreach (var allocationDto in allocations)
        {
            var type = await _context.InvestmentTypes.FindAsync(allocationDto.Id);
            if (type != null)
            {
                var allocation = await _context.UserInvestmentTypeAllocations
                    .FirstOrDefaultAsync(ua => ua.UserId == userId.Value && ua.InvestmentTypeId == allocationDto.Id);

                if (allocation == null)
                {
                    allocation = new UserInvestmentTypeAllocation
                    {
                        UserId = userId.Value,
                        InvestmentTypeId = allocationDto.Id,
                        AllocationPercentage = allocationDto.AllocationPercentage
                    };
                    _context.UserInvestmentTypeAllocations.Add(allocation);
                }
                else
                {
                    allocation.AllocationPercentage = allocationDto.AllocationPercentage;
                }
            }
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
