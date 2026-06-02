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
public class InvestmentTransactionsController : ControllerBase
{
    private readonly PortfolioDbContext _context;
    private readonly AuthService _authService;

    public InvestmentTransactionsController(PortfolioDbContext context, AuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    [HttpGet("investment/{investmentId}")]
    public async Task<ActionResult<IEnumerable<InvestmentTransactionDto>>> GetInvestmentTransactions(
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

        var query = _context.InvestmentTransactions
            .Where(t => t.InvestmentId == investmentId);

        if (startDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= endDate.Value);
        }

        var transactions = await query
            .OrderBy(t => t.TransactionDate)
            .Select(t => new InvestmentTransactionDto
            {
                Id = t.Id,
                InvestmentId = t.InvestmentId,
                Type = t.Type,
                Amount = t.Amount,
                UnitValue = t.UnitValue,
                Quantity = t.Quantity,
                TransactionDate = t.TransactionDate,
                Notes = t.Notes,
                InvestmentName = investment.Name
            })
            .ToListAsync();

        return Ok(transactions);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InvestmentTransactionDto>>> GetAllUserTransactions(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        // Get all user investment IDs
        var userInvestmentIds = await _context.Investments
            .Where(i => i.UserId == userId.Value)
            .Select(i => i.Id)
            .ToListAsync();

        if (!userInvestmentIds.Any())
        {
            return Ok(new List<InvestmentTransactionDto>());
        }

        var query = _context.InvestmentTransactions
            .Include(t => t.Investment)
            .Where(t => userInvestmentIds.Contains(t.InvestmentId));

        if (startDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= endDate.Value);
        }

        var transactions = await query
            .OrderBy(t => t.TransactionDate)
            .Select(t => new InvestmentTransactionDto
            {
                Id = t.Id,
                InvestmentId = t.InvestmentId,
                Type = t.Type,
                Amount = t.Amount,
                UnitValue = t.UnitValue,
                Quantity = t.Quantity,
                TransactionDate = t.TransactionDate,
                Notes = t.Notes,
                InvestmentName = t.Investment!.Name
            })
            .ToListAsync();

        return Ok(transactions);
    }

    [HttpPost]
    public async Task<ActionResult<InvestmentTransactionDto>> CreateTransaction([FromBody] CreateInvestmentTransactionDto dto)
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

        var transaction = new InvestmentTransaction
        {
            InvestmentId = dto.InvestmentId,
            Type = dto.Type,
            Amount = dto.Amount,
            UnitValue = dto.UnitValue,
            Quantity = dto.Quantity,
            TransactionDate = dto.TransactionDate ?? DateTime.UtcNow,
            Notes = dto.Notes
        };

        _context.InvestmentTransactions.Add(transaction);

        // Keep the investment's Quantity / CurrentValue in sync with the movement so that
        // profitability calculations and the UI see a consistent closed/open state.
        // CurrentValue is updated additively (Deposit adds the Amount, Withdrawal subtracts it)
        // rather than recomputed as Quantity * UnitValue. The Q*UV formula only works when
        // UnitValue is a per-unit market price (stocks/ETFs); for accumulating accounts
        // (retirement, savings, CDB) the user treats Quantity as a marker and UnitValue as the
        // running balance, so recomputing would overwrite the real value with the deposit-level
        // unit price. UnitValue is intentionally left untouched here — the user (or the price
        // feed) updates it via the Investment edit flow when it makes sense.
        bool investmentStateChanged = false;
        if (dto.Type == TransactionType.Withdrawal || dto.Type == TransactionType.Deposit)
        {
            decimal? quantity = dto.Quantity;
            if (!quantity.HasValue && dto.UnitValue.HasValue && dto.UnitValue.Value > 0)
            {
                quantity = dto.Amount / dto.UnitValue.Value;
            }

            if (quantity.HasValue && quantity.Value > 0)
            {
                decimal currentQuantity = investment.Quantity ?? 0m;
                decimal newQuantity = dto.Type == TransactionType.Withdrawal
                    ? currentQuantity - quantity.Value
                    : currentQuantity + quantity.Value;

                // Clamp to zero on full liquidation to avoid negative quantities from rounding
                if (newQuantity < 0m)
                {
                    newQuantity = 0m;
                }

                decimal newCurrentValue = dto.Type == TransactionType.Withdrawal
                    ? investment.CurrentValue - dto.Amount
                    : investment.CurrentValue + dto.Amount;

                // Full liquidation (no units left) forces CurrentValue to 0 so the position
                // disappears from totals even if rounding leaves a small residual.
                if (newQuantity == 0m || newCurrentValue < 0m)
                {
                    newCurrentValue = 0m;
                }

                investment.Quantity = newQuantity;
                investment.CurrentValue = newCurrentValue;
                investment.LastUpdatedDate = transaction.TransactionDate;
                investmentStateChanged = true;
            }
        }

        await _context.SaveChangesAsync();

        // Record a history entry so the time series reflects the post-transaction state.
        if (investmentStateChanged)
        {
            var history = new InvestmentHistory
            {
                InvestmentId = investment.Id,
                Value = investment.CurrentValue,
                UnitValue = investment.UnitValue,
                Quantity = investment.Quantity,
                RecordedDate = transaction.TransactionDate,
                Notes = $"{transaction.Type} recorded via transaction"
            };
            _context.InvestmentHistories.Add(history);
            await _context.SaveChangesAsync();
        }

        var result = new InvestmentTransactionDto
        {
            Id = transaction.Id,
            InvestmentId = transaction.InvestmentId,
            Type = transaction.Type,
            Amount = transaction.Amount,
            UnitValue = transaction.UnitValue,
            Quantity = transaction.Quantity,
            TransactionDate = transaction.TransactionDate,
            Notes = transaction.Notes,
            InvestmentName = investment.Name
        };

        return CreatedAtAction(nameof(GetInvestmentTransactions), new { investmentId = transaction.InvestmentId }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<InvestmentTransactionDto>> UpdateTransaction(int id, [FromBody] UpdateInvestmentTransactionDto dto)
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        var transaction = await _context.InvestmentTransactions
            .Include(t => t.Investment)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction == null || transaction.Investment?.UserId != userId.Value)
        {
            return NotFound();
        }

        transaction.Type = dto.Type;
        transaction.Amount = dto.Amount;
        transaction.UnitValue = dto.UnitValue;
        transaction.Quantity = dto.Quantity;
        transaction.TransactionDate = dto.TransactionDate ?? transaction.TransactionDate;
        transaction.Notes = dto.Notes;

        await _context.SaveChangesAsync();

        var result = new InvestmentTransactionDto
        {
            Id = transaction.Id,
            InvestmentId = transaction.InvestmentId,
            Type = transaction.Type,
            Amount = transaction.Amount,
            UnitValue = transaction.UnitValue,
            Quantity = transaction.Quantity,
            TransactionDate = transaction.TransactionDate,
            Notes = transaction.Notes,
            InvestmentName = transaction.Investment!.Name
        };

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(int id)
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        var transaction = await _context.InvestmentTransactions
            .Include(t => t.Investment)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction == null || transaction.Investment?.UserId != userId.Value)
        {
            return NotFound();
        }

        _context.InvestmentTransactions.Remove(transaction);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
