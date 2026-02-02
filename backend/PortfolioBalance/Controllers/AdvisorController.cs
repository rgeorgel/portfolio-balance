using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioBalance.Data;
using PortfolioBalance.DTOs;
using PortfolioBalance.Models;
using PortfolioBalance.Services;

namespace PortfolioBalance.Controllers;

[ApiController]
[Route("api/advisor")]
[Authorize]
public class AdvisorController : ControllerBase
{
    private readonly PortfolioDbContext _context;
    private readonly AdvisorAuthService _advisorAuthService;

    public AdvisorController(PortfolioDbContext context, AdvisorAuthService advisorAuthService)
    {
        _context = context;
        _advisorAuthService = advisorAuthService;
    }

    private async Task<int?> GetAdvisorIdAsync()
    {
        if (!_advisorAuthService.IsAdvisor(User))
        {
            return null;
        }
        return _advisorAuthService.GetAdvisorIdFromToken(User);
    }

    private async Task<bool> IsClientOfAdvisor(int advisorId, int userId)
    {
        return await _context.AdvisorClients.AnyAsync(ac =>
            ac.AdvisorId == advisorId && ac.UserId == userId && ac.IsActive);
    }

    [HttpGet("clients")]
    public async Task<ActionResult<List<AdvisorClientDto>>> GetClients()
    {
        var advisorId = await GetAdvisorIdAsync();
        if (advisorId == null)
        {
            return Unauthorized(new { message = "Only advisors can access this endpoint" });
        }

        var clients = await _context.AdvisorClients
            .Where(ac => ac.AdvisorId == advisorId && ac.IsActive)
            .Include(ac => ac.User)
                .ThenInclude(u => u.Investments)
            .Select(ac => new AdvisorClientDto
            {
                Id = ac.Id,
                UserId = ac.UserId,
                Username = ac.User.Username,
                Email = ac.User.Email,
                AssignedDate = ac.AssignedDate,
                IsActive = ac.IsActive,
                TotalPortfolioValue = ac.User.Investments.Sum(i => i.CurrentValue),
                TotalInvestments = ac.User.Investments.Count
            })
            .ToListAsync();

        return Ok(clients);
    }

    [HttpPost("clients")]
    public async Task<ActionResult<AdvisorClientDto>> AddClient(AddClientDto addClientDto)
    {
        var advisorId = await GetAdvisorIdAsync();
        if (advisorId == null)
        {
            return Unauthorized(new { message = "Only advisors can access this endpoint" });
        }

        // Find user by username or email
        var user = await _context.Users
            .Include(u => u.Investments)
            .FirstOrDefaultAsync(u =>
                u.Username == addClientDto.UsernameOrEmail || u.Email == addClientDto.UsernameOrEmail);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        // Check if already a client
        var existingRelation = await _context.AdvisorClients
            .FirstOrDefaultAsync(ac => ac.AdvisorId == advisorId && ac.UserId == user.Id);

        if (existingRelation != null)
        {
            if (existingRelation.IsActive)
            {
                return BadRequest(new { message = "User is already your client" });
            }
            // Reactivate the relationship
            existingRelation.IsActive = true;
            existingRelation.AssignedDate = DateTime.UtcNow;
        }
        else
        {
            // Create new relationship
            var advisorClient = new AdvisorClient
            {
                AdvisorId = advisorId.Value,
                UserId = user.Id
            };
            _context.AdvisorClients.Add(advisorClient);
        }

        await _context.SaveChangesAsync();

        return Ok(new AdvisorClientDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            AssignedDate = DateTime.UtcNow,
            IsActive = true,
            TotalPortfolioValue = user.Investments.Sum(i => i.CurrentValue),
            TotalInvestments = user.Investments.Count
        });
    }

    [HttpDelete("clients/{userId}")]
    public async Task<ActionResult> RemoveClient(int userId)
    {
        var advisorId = await GetAdvisorIdAsync();
        if (advisorId == null)
        {
            return Unauthorized(new { message = "Only advisors can access this endpoint" });
        }

        var relation = await _context.AdvisorClients
            .FirstOrDefaultAsync(ac => ac.AdvisorId == advisorId && ac.UserId == userId && ac.IsActive);

        if (relation == null)
        {
            return NotFound(new { message = "Client not found" });
        }

        relation.IsActive = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("clients/{userId}/portfolio")]
    public async Task<ActionResult<ClientPortfolioSummaryDto>> GetClientPortfolio(int userId)
    {
        var advisorId = await GetAdvisorIdAsync();
        if (advisorId == null)
        {
            return Unauthorized(new { message = "Only advisors can access this endpoint" });
        }

        if (!await IsClientOfAdvisor(advisorId.Value, userId))
        {
            return Forbidden();
        }

        var user = await _context.Users
            .Include(u => u.Investments)
                .ThenInclude(i => i.InvestmentType)
            .Include(u => u.InvestmentTypeAllocations)
                .ThenInclude(a => a.InvestmentType)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var investmentTypes = await _context.InvestmentTypes.ToListAsync();
        var totalValue = user.Investments.Sum(i => i.CurrentValue);

        var investmentTypeDtos = investmentTypes.Select(type =>
        {
            var allocation = user.InvestmentTypeAllocations
                .FirstOrDefault(a => a.InvestmentTypeId == type.Id);
            var typeInvestments = user.Investments
                .Where(i => i.InvestmentTypeId == type.Id);
            var typeValue = typeInvestments.Sum(i => i.CurrentValue);

            return new InvestmentTypeDto
            {
                Id = type.Id,
                Name = type.Name,
                AllocationPercentage = allocation?.AllocationPercentage ?? 0,
                CurrentValue = typeValue,
                CurrentPercentage = totalValue > 0 ? (typeValue / totalValue * 100) : 0
            };
        }).ToList();

        var investmentDtos = user.Investments.Select(i => new InvestmentDto
        {
            Id = i.Id,
            Name = i.Name,
            InvestmentTypeId = i.InvestmentTypeId,
            InvestmentTypeName = i.InvestmentType?.Name ?? "",
            CurrentValue = i.CurrentValue,
            UnitValue = i.UnitValue,
            Quantity = i.Quantity,
            Weight = i.Weight,
            CreatedDate = i.CreatedDate,
            LastUpdatedDate = i.LastUpdatedDate
        }).ToList();

        return Ok(new ClientPortfolioSummaryDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            TotalValue = totalValue,
            TotalInvestments = user.Investments.Count,
            InvestmentTypes = investmentTypeDtos,
            Investments = investmentDtos
        });
    }

    [HttpGet("clients/{userId}/investments")]
    public async Task<ActionResult<List<InvestmentDto>>> GetClientInvestments(int userId, [FromQuery] int? investmentTypeId = null)
    {
        var advisorId = await GetAdvisorIdAsync();
        if (advisorId == null)
        {
            return Unauthorized(new { message = "Only advisors can access this endpoint" });
        }

        if (!await IsClientOfAdvisor(advisorId.Value, userId))
        {
            return Forbidden();
        }

        var query = _context.Investments
            .Include(i => i.InvestmentType)
            .Where(i => i.UserId == userId);

        if (investmentTypeId.HasValue)
        {
            query = query.Where(i => i.InvestmentTypeId == investmentTypeId.Value);
        }

        var investments = await query
            .OrderByDescending(i => i.CurrentValue)
            .Select(i => new InvestmentDto
            {
                Id = i.Id,
                Name = i.Name,
                InvestmentTypeId = i.InvestmentTypeId,
                InvestmentTypeName = i.InvestmentType != null ? i.InvestmentType.Name : "",
                CurrentValue = i.CurrentValue,
                UnitValue = i.UnitValue,
                Quantity = i.Quantity,
                Weight = i.Weight,
                CreatedDate = i.CreatedDate,
                LastUpdatedDate = i.LastUpdatedDate
            })
            .ToListAsync();

        return Ok(investments);
    }

    [HttpGet("clients/{userId}/investmenttypes")]
    public async Task<ActionResult<List<InvestmentTypeDto>>> GetClientInvestmentTypes(int userId)
    {
        var advisorId = await GetAdvisorIdAsync();
        if (advisorId == null)
        {
            return Unauthorized(new { message = "Only advisors can access this endpoint" });
        }

        if (!await IsClientOfAdvisor(advisorId.Value, userId))
        {
            return Forbidden();
        }

        var investmentTypes = await _context.InvestmentTypes.ToListAsync();
        var userAllocations = await _context.UserInvestmentTypeAllocations
            .Where(a => a.UserId == userId)
            .ToListAsync();
        var userInvestments = await _context.Investments
            .Where(i => i.UserId == userId)
            .ToListAsync();

        var totalValue = userInvestments.Sum(i => i.CurrentValue);

        var result = investmentTypes.Select(type =>
        {
            var allocation = userAllocations.FirstOrDefault(a => a.InvestmentTypeId == type.Id);
            var typeInvestments = userInvestments.Where(i => i.InvestmentTypeId == type.Id);
            var typeValue = typeInvestments.Sum(i => i.CurrentValue);

            return new InvestmentTypeDto
            {
                Id = type.Id,
                Name = type.Name,
                AllocationPercentage = allocation?.AllocationPercentage ?? 0,
                CurrentValue = typeValue,
                CurrentPercentage = totalValue > 0 ? (typeValue / totalValue * 100) : 0
            };
        }).ToList();

        return Ok(result);
    }

    [HttpGet("clients/{userId}/transactions")]
    public async Task<ActionResult<List<InvestmentTransactionDto>>> GetClientTransactions(
        int userId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var advisorId = await GetAdvisorIdAsync();
        if (advisorId == null)
        {
            return Unauthorized(new { message = "Only advisors can access this endpoint" });
        }

        if (!await IsClientOfAdvisor(advisorId.Value, userId))
        {
            return Forbidden();
        }

        var query = _context.InvestmentTransactions
            .Include(t => t.Investment)
            .Where(t => t.Investment.UserId == userId);

        if (startDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= endDate.Value);
        }

        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .Select(t => new InvestmentTransactionDto
            {
                Id = t.Id,
                InvestmentId = t.InvestmentId,
                InvestmentName = t.Investment.Name,
                Type = t.Type,
                Amount = t.Amount,
                UnitValue = t.UnitValue,
                Quantity = t.Quantity,
                TransactionDate = t.TransactionDate,
                Notes = t.Notes
            })
            .ToListAsync();

        return Ok(transactions);
    }

    [HttpGet("clients/{userId}/history")]
    public async Task<ActionResult<List<InvestmentHistoryDto>>> GetClientHistory(
        int userId,
        [FromQuery] int? investmentId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var advisorId = await GetAdvisorIdAsync();
        if (advisorId == null)
        {
            return Unauthorized(new { message = "Only advisors can access this endpoint" });
        }

        if (!await IsClientOfAdvisor(advisorId.Value, userId))
        {
            return Forbidden();
        }

        var query = _context.InvestmentHistories
            .Include(h => h.Investment)
            .Where(h => h.Investment.UserId == userId);

        if (investmentId.HasValue)
        {
            query = query.Where(h => h.InvestmentId == investmentId.Value);
        }
        if (startDate.HasValue)
        {
            query = query.Where(h => h.RecordedDate >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            query = query.Where(h => h.RecordedDate <= endDate.Value);
        }

        var history = await query
            .OrderByDescending(h => h.RecordedDate)
            .Select(h => new InvestmentHistoryDto
            {
                Id = h.Id,
                InvestmentId = h.InvestmentId,
                InvestmentName = h.Investment.Name,
                Value = h.Value,
                UnitValue = h.UnitValue,
                Quantity = h.Quantity,
                RecordedDate = h.RecordedDate,
                Notes = h.Notes
            })
            .ToListAsync();

        return Ok(history);
    }

    private ActionResult Forbidden()
    {
        return StatusCode(403, new { message = "You don't have permission to view this client's data" });
    }
}
