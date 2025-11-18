using Microsoft.AspNetCore.Mvc;
using PortfolioBalance.DTOs;
using PortfolioBalance.Services;

namespace PortfolioBalance.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PortfolioController : ControllerBase
{
    private readonly PortfolioBalancingService _balancingService;

    public PortfolioController(PortfolioBalancingService balancingService)
    {
        _balancingService = balancingService;
    }

    [HttpPost("calculate-balance")]
    public async Task<ActionResult<BalanceCalculationResponseDto>> CalculateBalance([FromBody] BalanceCalculationRequestDto request)
    {
        if (request.NewInvestmentAmount <= 0)
        {
            return BadRequest("Investment amount must be greater than zero");
        }

        var result = await _balancingService.CalculateBalanceAsync(request.NewInvestmentAmount);
        return Ok(result);
    }
}
