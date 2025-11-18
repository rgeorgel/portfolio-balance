using Microsoft.EntityFrameworkCore;
using PortfolioBalance.Data;
using PortfolioBalance.DTOs;

namespace PortfolioBalance.Services;

public class PortfolioBalancingService
{
    private readonly PortfolioDbContext _context;

    public PortfolioBalancingService(PortfolioDbContext context)
    {
        _context = context;
    }

    public async Task<BalanceCalculationResponseDto> CalculateBalanceAsync(decimal newInvestmentAmount)
    {
        var investmentTypes = await _context.InvestmentTypes
            .Include(t => t.Investments)
            .ToListAsync();

        var totalCurrentValue = investmentTypes
            .Sum(t => t.Investments.Sum(i => i.CurrentValue));

        var totalAfterInvestment = totalCurrentValue + newInvestmentAmount;

        var response = new BalanceCalculationResponseDto
        {
            TotalPortfolioValue = totalCurrentValue,
            NewInvestmentAmount = newInvestmentAmount,
            TotalAfterInvestment = totalAfterInvestment,
            Allocations = new List<TypeAllocationDto>()
        };

        foreach (var type in investmentTypes)
        {
            var currentTypeValue = type.Investments.Sum(i => i.CurrentValue);
            var currentPercentage = totalCurrentValue > 0
                ? (currentTypeValue / totalCurrentValue) * 100
                : 0;

            var targetValue = totalAfterInvestment * (type.AllocationPercentage / 100);
            var amountToInvest = Math.Max(0, targetValue - currentTypeValue);

            var valueAfterInvestment = currentTypeValue + amountToInvest;
            var percentageAfterInvestment = totalAfterInvestment > 0
                ? (valueAfterInvestment / totalAfterInvestment) * 100
                : 0;

            var typeAllocation = new TypeAllocationDto
            {
                InvestmentTypeId = type.Id,
                InvestmentTypeName = type.Name,
                TargetPercentage = type.AllocationPercentage,
                CurrentValue = currentTypeValue,
                CurrentPercentage = currentPercentage,
                AmountToInvest = amountToInvest,
                ValueAfterInvestment = valueAfterInvestment,
                PercentageAfterInvestment = percentageAfterInvestment,
                InvestmentAllocations = new List<InvestmentAllocationDto>()
            };

            // Calculate allocation within investments of this type based on weights
            if (amountToInvest > 0 && type.Investments.Any())
            {
                var totalWeight = type.Investments.Sum(i => i.Weight);

                foreach (var investment in type.Investments)
                {
                    var weightPercentage = totalWeight > 0
                        ? investment.Weight / totalWeight
                        : 0;

                    var investmentAmount = amountToInvest * weightPercentage;

                    typeAllocation.InvestmentAllocations.Add(new InvestmentAllocationDto
                    {
                        InvestmentId = investment.Id,
                        InvestmentName = investment.Name,
                        Weight = investment.Weight,
                        CurrentValue = investment.CurrentValue,
                        AmountToInvest = investmentAmount,
                        ValueAfterInvestment = investment.CurrentValue + investmentAmount
                    });
                }
            }

            response.Allocations.Add(typeAllocation);
        }

        return response;
    }
}
