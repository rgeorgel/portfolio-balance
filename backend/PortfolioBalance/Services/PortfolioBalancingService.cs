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

    public async Task<BalanceCalculationResponseDto> CalculateBalanceAsync(int userId, decimal newInvestmentAmount)
    {
        // Get all investment types with their user-specific allocations and investments
        var investmentTypes = await _context.InvestmentTypes
            .Include(t => t.Investments.Where(i => i.UserId == userId))
            .Include(t => t.UserAllocations.Where(ua => ua.UserId == userId))
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

        // First pass: calculate deficits (how far each type is from target)
        var typeDeficits = new List<(int typeId, decimal currentValue, decimal deficit, decimal targetPercentage)>();

        foreach (var type in investmentTypes)
        {
            var currentTypeValue = type.Investments.Sum(i => i.CurrentValue);
            var userAllocation = type.UserAllocations.FirstOrDefault();
            var targetPercentage = userAllocation?.AllocationPercentage ?? 0m;
            var targetValue = totalAfterInvestment * (targetPercentage / 100);
            var deficit = Math.Max(0, targetValue - currentTypeValue);

            typeDeficits.Add((type.Id, currentTypeValue, deficit, targetPercentage));
        }

        // Calculate total deficit across all types
        var totalDeficit = typeDeficits.Sum(td => td.deficit);

        // Second pass: allocate the available investment proportionally to deficits
        decimal remainingToAllocate = newInvestmentAmount;
        var allocations = new Dictionary<int, decimal>();

        if (totalDeficit > 0)
        {
            // Distribute proportionally based on deficits
            for (int i = 0; i < typeDeficits.Count; i++)
            {
                var typeDeficit = typeDeficits[i];
                decimal amountToInvest;

                if (i == typeDeficits.Count - 1)
                {
                    // Last item gets the remaining amount to avoid rounding issues
                    amountToInvest = remainingToAllocate;
                }
                else
                {
                    // Proportional allocation based on deficit
                    amountToInvest = (typeDeficit.deficit / totalDeficit) * newInvestmentAmount;
                    remainingToAllocate -= amountToInvest;
                }

                allocations[typeDeficit.typeId] = amountToInvest;
            }
        }
        else
        {
            // No deficits - distribute proportionally based on target percentages
            for (int i = 0; i < typeDeficits.Count; i++)
            {
                var typeDeficit = typeDeficits[i];
                decimal amountToInvest;

                if (i == typeDeficits.Count - 1)
                {
                    // Last item gets the remaining amount to avoid rounding issues
                    amountToInvest = remainingToAllocate;
                }
                else if (typeDeficit.targetPercentage > 0)
                {
                    // Proportional allocation based on target percentage
                    amountToInvest = (typeDeficit.targetPercentage / 100) * newInvestmentAmount;
                    remainingToAllocate -= amountToInvest;
                }
                else
                {
                    amountToInvest = 0;
                }

                allocations[typeDeficit.typeId] = amountToInvest;
            }
        }

        // Third pass: build response with allocated amounts
        foreach (var type in investmentTypes)
        {
            var currentTypeValue = type.Investments.Sum(i => i.CurrentValue);
            var currentPercentage = totalCurrentValue > 0
                ? (currentTypeValue / totalCurrentValue) * 100
                : 0;

            var userAllocation = type.UserAllocations.FirstOrDefault();
            var targetPercentage = userAllocation?.AllocationPercentage ?? 0m;

            var amountToInvest = allocations.ContainsKey(type.Id) ? allocations[type.Id] : 0;
            var valueAfterInvestment = currentTypeValue + amountToInvest;
            var percentageAfterInvestment = totalAfterInvestment > 0
                ? (valueAfterInvestment / totalAfterInvestment) * 100
                : 0;

            var typeAllocation = new TypeAllocationDto
            {
                InvestmentTypeId = type.Id,
                InvestmentTypeName = type.Name,
                TargetPercentage = targetPercentage,
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
                decimal remainingInvestmentAmount = amountToInvest;

                var investmentsList = type.Investments.ToList();

                // Find the last investment with weight > 0 (to assign remaining amount for rounding)
                var lastWeightedIndex = -1;
                for (int i = investmentsList.Count - 1; i >= 0; i--)
                {
                    if (investmentsList[i].Weight > 0)
                    {
                        lastWeightedIndex = i;
                        break;
                    }
                }

                for (int i = 0; i < investmentsList.Count; i++)
                {
                    var investment = investmentsList[i];
                    decimal investmentAmount;

                    if (investment.Weight == 0)
                    {
                        // Investments with weight 0 are not counted in allocation
                        investmentAmount = 0;
                    }
                    else if (i == lastWeightedIndex)
                    {
                        // Last weighted investment gets remaining amount to avoid rounding issues
                        investmentAmount = remainingInvestmentAmount;
                    }
                    else
                    {
                        var weightPercentage = totalWeight > 0
                            ? investment.Weight / totalWeight
                            : 0;
                        investmentAmount = amountToInvest * weightPercentage;
                        remainingInvestmentAmount -= investmentAmount;
                    }

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
