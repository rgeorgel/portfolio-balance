using Microsoft.EntityFrameworkCore;
using PortfolioBalance.Data;
using PortfolioBalance.DTOs;

namespace PortfolioBalance.Services;

public class ProfitabilityService
{
    private readonly PortfolioDbContext _context;

    public ProfitabilityService(PortfolioDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Calculates profitability for a specific investment
    /// </summary>
    public async Task<InvestmentProfitabilityDto?> GetInvestmentProfitabilityAsync(int userId, int investmentId)
    {
        var investment = await _context.Investments
            .Include(i => i.InvestmentType)
            .Include(i => i.InvestmentHistory)
            .FirstOrDefaultAsync(i => i.Id == investmentId && i.UserId == userId);

        if (investment == null)
        {
            return null;
        }

        var history = investment.InvestmentHistory
            .OrderBy(h => h.RecordedDate)
            .ToList();

        if (history.Count == 0)
        {
            // No history, use current value as both initial and current
            return new InvestmentProfitabilityDto
            {
                InvestmentId = investment.Id,
                InvestmentName = investment.Name,
                InvestmentTypeName = investment.InvestmentType.Name,
                InitialValue = investment.CurrentValue,
                CurrentValue = investment.CurrentValue,
                AbsoluteReturn = 0,
                ReturnPercentage = 0,
                AnnualizedReturn = 0,
                InvestmentPeriodDays = 0,
                FirstRecordedDate = investment.CreatedDate,
                LastRecordedDate = DateTime.UtcNow
            };
        }

        var firstRecord = history.First();
        var lastRecord = history.Last();
        var initialValue = firstRecord.Value;
        var currentValue = lastRecord.Value;
        var firstDate = firstRecord.RecordedDate;
        var lastDate = lastRecord.RecordedDate;
        var periodDays = (int)(lastDate - firstDate).TotalDays;

        var absoluteReturn = currentValue - initialValue;
        var returnPercentage = initialValue != 0 ? (absoluteReturn / initialValue) * 100 : 0;
        var annualizedReturn = CalculateAnnualizedReturn(initialValue, currentValue, periodDays);

        return new InvestmentProfitabilityDto
        {
            InvestmentId = investment.Id,
            InvestmentName = investment.Name,
            InvestmentTypeName = investment.InvestmentType.Name,
            InitialValue = initialValue,
            CurrentValue = currentValue,
            AbsoluteReturn = absoluteReturn,
            ReturnPercentage = returnPercentage,
            AnnualizedReturn = annualizedReturn,
            InvestmentPeriodDays = periodDays,
            FirstRecordedDate = firstDate,
            LastRecordedDate = lastDate
        };
    }

    /// <summary>
    /// Calculates overall portfolio profitability with breakdowns
    /// </summary>
    public async Task<PortfolioProfitabilityDto> GetPortfolioProfitabilityAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var investments = await _context.Investments
            .Include(i => i.InvestmentType)
            .Include(i => i.InvestmentHistory)
            .Where(i => i.UserId == userId)
            .ToListAsync();

        var investmentProfitabilities = new List<InvestmentProfitabilityDto>();
        decimal totalInitialValue = 0;
        decimal totalCurrentValue = 0;
        DateTime? earliestDate = null;
        DateTime? latestDate = null;

        foreach (var investment in investments)
        {
            var history = investment.InvestmentHistory
                .Where(h => (!startDate.HasValue || h.RecordedDate >= startDate.Value) &&
                           (!endDate.HasValue || h.RecordedDate <= endDate.Value))
                .OrderBy(h => h.RecordedDate)
                .ToList();

            if (history.Count == 0)
            {
                // No history in this date range, skip or use current value
                continue;
            }

            var firstRecord = history.First();
            var lastRecord = history.Last();
            var initialValue = firstRecord.Value;
            var currentValue = lastRecord.Value;
            var firstDate = firstRecord.RecordedDate;
            var lastDate = lastRecord.RecordedDate;
            var periodDays = (int)(lastDate - firstDate).TotalDays;

            var absoluteReturn = currentValue - initialValue;
            var returnPercentage = initialValue != 0 ? (absoluteReturn / initialValue) * 100 : 0;
            var annualizedReturn = CalculateAnnualizedReturn(initialValue, currentValue, periodDays);

            investmentProfitabilities.Add(new InvestmentProfitabilityDto
            {
                InvestmentId = investment.Id,
                InvestmentName = investment.Name,
                InvestmentTypeName = investment.InvestmentType.Name,
                InitialValue = initialValue,
                CurrentValue = currentValue,
                AbsoluteReturn = absoluteReturn,
                ReturnPercentage = returnPercentage,
                AnnualizedReturn = annualizedReturn,
                InvestmentPeriodDays = periodDays,
                FirstRecordedDate = firstDate,
                LastRecordedDate = lastDate
            });

            totalInitialValue += initialValue;
            totalCurrentValue += currentValue;

            if (!earliestDate.HasValue || firstDate < earliestDate.Value)
                earliestDate = firstDate;
            if (!latestDate.HasValue || lastDate > latestDate.Value)
                latestDate = lastDate;
        }

        var totalAbsoluteReturn = totalCurrentValue - totalInitialValue;
        var totalReturnPercentage = totalInitialValue != 0 ? (totalAbsoluteReturn / totalInitialValue) * 100 : 0;
        var totalPeriodDays = earliestDate.HasValue && latestDate.HasValue
            ? (int)(latestDate.Value - earliestDate.Value).TotalDays
            : 0;
        var totalAnnualizedReturn = CalculateAnnualizedReturn(totalInitialValue, totalCurrentValue, totalPeriodDays);

        // Group by investment type
        var profitabilityByType = investmentProfitabilities
            .GroupBy(ip => new { ip.InvestmentTypeName })
            .Select(g =>
            {
                var typeInitialValue = g.Sum(ip => ip.InitialValue);
                var typeCurrentValue = g.Sum(ip => ip.CurrentValue);
                var typeAbsoluteReturn = typeCurrentValue - typeInitialValue;
                var typeReturnPercentage = typeInitialValue != 0 ? (typeAbsoluteReturn / typeInitialValue) * 100 : 0;
                var avgPeriodDays = g.Any() ? (int)g.Average(ip => ip.InvestmentPeriodDays) : 0;
                var typeAnnualizedReturn = CalculateAnnualizedReturn(typeInitialValue, typeCurrentValue, avgPeriodDays);

                return new InvestmentTypeProfitabilityDto
                {
                    InvestmentTypeId = 0, // Will be set if needed
                    InvestmentTypeName = g.Key.InvestmentTypeName,
                    InitialValue = typeInitialValue,
                    CurrentValue = typeCurrentValue,
                    AbsoluteReturn = typeAbsoluteReturn,
                    ReturnPercentage = typeReturnPercentage,
                    AnnualizedReturn = typeAnnualizedReturn,
                    InvestmentCount = g.Count()
                };
            })
            .OrderByDescending(t => t.ReturnPercentage)
            .ToList();

        return new PortfolioProfitabilityDto
        {
            TotalInitialValue = totalInitialValue,
            TotalCurrentValue = totalCurrentValue,
            TotalAbsoluteReturn = totalAbsoluteReturn,
            TotalReturnPercentage = totalReturnPercentage,
            TotalAnnualizedReturn = totalAnnualizedReturn,
            TotalInvestments = investmentProfitabilities.Count,
            EarliestInvestmentDate = earliestDate,
            LatestUpdateDate = latestDate,
            InvestmentsByPerformance = investmentProfitabilities.OrderByDescending(ip => ip.ReturnPercentage).ToList(),
            ProfitabilityByType = profitabilityByType
        };
    }

    /// <summary>
    /// Gets top and bottom performing investments
    /// </summary>
    public async Task<TopPerformersDto> GetTopPerformersAsync(int userId, int limit = 5)
    {
        var portfolioProfitability = await GetPortfolioProfitabilityAsync(userId);

        var topPerformers = portfolioProfitability.InvestmentsByPerformance
            .OrderByDescending(ip => ip.ReturnPercentage)
            .Take(limit)
            .ToList();

        var bottomPerformers = portfolioProfitability.InvestmentsByPerformance
            .OrderBy(ip => ip.ReturnPercentage)
            .Take(limit)
            .ToList();

        return new TopPerformersDto
        {
            TopPerformers = topPerformers,
            BottomPerformers = bottomPerformers
        };
    }

    /// <summary>
    /// Calculates profitability grouped by investment type
    /// </summary>
    public async Task<List<InvestmentTypeProfitabilityDto>> GetProfitabilityByTypeAsync(int userId)
    {
        var portfolioProfitability = await GetPortfolioProfitabilityAsync(userId);
        return portfolioProfitability.ProfitabilityByType;
    }

    /// <summary>
    /// Calculates annualized return based on initial value, current value, and time period
    /// Formula: ((Current / Initial) ^ (365.25 / Days) - 1) * 100
    /// </summary>
    private decimal CalculateAnnualizedReturn(decimal initialValue, decimal currentValue, int periodDays)
    {
        if (initialValue <= 0 || currentValue <= 0 || periodDays <= 0)
        {
            return 0;
        }

        // For very short periods (< 30 days), return simple percentage to avoid misleading annualization
        if (periodDays < 30)
        {
            return initialValue != 0 ? ((currentValue - initialValue) / initialValue) * 100 : 0;
        }

        var years = periodDays / 365.25;
        var growthFactor = (double)(currentValue / initialValue);
        var annualizedMultiplier = Math.Pow(growthFactor, 1.0 / years);
        var annualizedReturn = (decimal)(annualizedMultiplier - 1.0) * 100;

        return annualizedReturn;
    }
}
