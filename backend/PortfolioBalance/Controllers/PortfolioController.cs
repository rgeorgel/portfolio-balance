using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioBalance.DTOs;
using PortfolioBalance.Services;

namespace PortfolioBalance.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PortfolioController : ControllerBase
{
    private readonly PortfolioBalancingService _balancingService;
    private readonly AuthService _authService;

    public PortfolioController(PortfolioBalancingService balancingService, AuthService authService)
    {
        _balancingService = balancingService;
        _authService = authService;
    }

    [HttpPost("calculate-balance")]
    public async Task<ActionResult<BalanceCalculationResponseDto>> CalculateBalance([FromBody] BalanceCalculationRequestDto request)
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        if (request.NewInvestmentAmount <= 0)
        {
            return BadRequest("Investment amount must be greater than zero");
        }

        var result = await _balancingService.CalculateBalanceAsync(userId.Value, request.NewInvestmentAmount);
        return Ok(result);
    }
}
