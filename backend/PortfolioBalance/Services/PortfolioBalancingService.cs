using Microsoft.EntityFrameworkCore;
using PortfolioBalance.Data;
using PortfolioBalance.DTOs;
using PortfolioBalance.Models;

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
                var weightedInvestments = type.Investments.Where(i => i.Weight > 0).ToList();

                if (weightedInvestments.Any())
                {
                    var totalWeight = weightedInvestments.Sum(i => i.Weight);
                    var totalTypeValue = type.Investments.Sum(i => i.CurrentValue);
                    var targetTypeValue = totalTypeValue + amountToInvest;

                    // Calculate deficit for each investment (how far it is from its target proportion)
                    var investmentDeficits = new List<(Investment investment, decimal deficit)>();

                    foreach (var investment in weightedInvestments)
                    {
                        var targetProportion = investment.Weight / totalWeight;
                        var targetValue = targetTypeValue * targetProportion;
                        var deficit = targetValue - investment.CurrentValue;

                        investmentDeficits.Add((investment, deficit));
                    }

                    // Sort by deficit descending (investments that need more money first)
                    investmentDeficits = investmentDeficits.OrderByDescending(x => x.deficit).ToList();

                    // Determine how many investments to select (2-4 based on amount available)
                    // This ensures we don't split the money too much and get amounts below minimum
                    int investmentsToSelect;
                    if (amountToInvest >= 2000)
                        investmentsToSelect = Math.Min(4, investmentDeficits.Count);
                    else if (amountToInvest >= 1000)
                        investmentsToSelect = Math.Min(3, investmentDeficits.Count);
                    else if (amountToInvest >= 500)
                        investmentsToSelect = Math.Min(2, investmentDeficits.Count);
                    else
                        investmentsToSelect = Math.Min(1, investmentDeficits.Count);

                    // Select top N investments with highest deficits
                    var selectedInvestments = investmentDeficits.Take(investmentsToSelect).ToList();

                    // Calculate total deficit of selected investments (use max to avoid division by zero)
                    var totalSelectedDeficit = Math.Max(selectedInvestments.Sum(x => x.deficit), 0.01m);

                    // Distribute the amount among selected investments proportionally to their deficits
                    decimal remainingInvestmentAmount = amountToInvest;

                    for (int i = 0; i < selectedInvestments.Count; i++)
                    {
                        var investmentInfo = selectedInvestments[i];
                        decimal investmentAmount;

                        if (i == selectedInvestments.Count - 1)
                        {
                            // Last investment gets remaining amount to avoid rounding issues
                            investmentAmount = remainingInvestmentAmount;
                        }
                        else
                        {
                            // Distribute proportionally to deficit
                            var deficitProportion = investmentInfo.deficit / totalSelectedDeficit;
                            investmentAmount = amountToInvest * deficitProportion;
                            remainingInvestmentAmount -= investmentAmount;
                        }

                        typeAllocation.InvestmentAllocations.Add(new InvestmentAllocationDto
                        {
                            InvestmentId = investmentInfo.investment.Id,
                            InvestmentName = investmentInfo.investment.Name,
                            Weight = investmentInfo.investment.Weight,
                            CurrentValue = investmentInfo.investment.CurrentValue,
                            AmountToInvest = investmentAmount,
                            ValueAfterInvestment = investmentInfo.investment.CurrentValue + investmentAmount,
                            UnitValue = investmentInfo.investment.UnitValue,
                            Quantity = investmentInfo.investment.Quantity
                        });
                    }
                }
            }

            response.Allocations.Add(typeAllocation);
        }

        return response;
    }
}
