using Microsoft.EntityFrameworkCore;
using PortfolioBalance.Data;
using PortfolioBalance.DTOs;
using PortfolioBalance.Models;

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
            .Include(i => i.InvestmentHistories)
            .Include(i => i.InvestmentTransactions)
            .FirstOrDefaultAsync(i => i.Id == investmentId && i.UserId == userId);

        if (investment == null)
        {
            return null;
        }

        // Check if investment has transactions registered
        var hasTransactions = investment.InvestmentTransactions.Any();

        if (hasTransactions)
        {
            // Use transaction-based calculation (accounts for deposits/withdrawals)
            return CalculateProfitabilityWithTransactions(investment);
        }
        else
        {
            // Use simple calculation based on history
            return CalculateProfitabilitySimple(investment);
        }
    }

    /// <summary>
    /// Calculates profitability using simple method (first vs last history record)
    /// </summary>
    private InvestmentProfitabilityDto CalculateProfitabilitySimple(Models.Investment investment)
    {
        var history = investment.InvestmentHistories
            .OrderBy(h => h.RecordedDate)
            .ToList();

        if (history.Count == 0)
        {
            // No history, use current value as both initial and current
            return new InvestmentProfitabilityDto
            {
                InvestmentId = investment.Id,
                InvestmentName = investment.Name,
                InvestmentTypeName = investment.InvestmentType?.Name ?? "Unknown",
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
            InvestmentTypeName = investment.InvestmentType?.Name ?? "Unknown",
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
    /// Calculates profitability considering transactions (deposits, withdrawals, dividends)
    /// Uses Money-Weighted Return approach
    /// </summary>
    private InvestmentProfitabilityDto CalculateProfitabilityWithTransactions(Models.Investment investment)
    {
        var transactions = investment.InvestmentTransactions
            .OrderBy(t => t.TransactionDate)
            .ToList();

        var history = investment.InvestmentHistories
            .OrderBy(h => h.RecordedDate)
            .ToList();

        if (transactions.Count == 0)
        {
            // No transactions, fallback to simple calculation
            return CalculateProfitabilitySimple(investment);
        }

        var firstTransaction = transactions.First();
        var lastTransaction = transactions.Last();
        var firstDate = firstTransaction.TransactionDate;
        var lastDate = history.Any() ? history.Last().RecordedDate : DateTime.UtcNow;

        // Calculate total invested (deposits - withdrawals)
        decimal totalDeposits = transactions
            .Where(t => t.Type == TransactionType.Deposit)
            .Sum(t => t.Amount);

        decimal totalWithdrawals = transactions
            .Where(t => t.Type == TransactionType.Withdrawal)
            .Sum(t => t.Amount);

        decimal totalDividends = transactions
            .Where(t => t.Type == TransactionType.Dividend)
            .Sum(t => t.Amount);

        decimal totalInvested = totalDeposits - totalWithdrawals;

        // Get current value from last history record or current investment value
        decimal currentValue = history.Any() ? history.Last().Value : investment.CurrentValue;

        // Calculate returns
        // Absolute return = (Current Value + Total Withdrawals + Total Dividends) - Total Deposits
        decimal absoluteReturn = (currentValue + totalWithdrawals + totalDividends) - totalDeposits;

        // Return percentage = Absolute Return / Total Invested * 100
        decimal returnPercentage = totalInvested != 0 ? (absoluteReturn / totalInvested) * 100 : 0;

        var periodDays = (int)(lastDate - firstDate).TotalDays;
        var annualizedReturn = CalculateAnnualizedReturn(totalInvested, currentValue, periodDays);

        return new InvestmentProfitabilityDto
        {
            InvestmentId = investment.Id,
            InvestmentName = investment.Name,
            InvestmentTypeName = investment.InvestmentType?.Name ?? "Unknown",
            InitialValue = totalInvested,
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
            .Include(i => i.InvestmentHistories)
            .Include(i => i.InvestmentTransactions)
            .Where(i => i.UserId == userId)
            .ToListAsync();

        var investmentProfitabilities = new List<InvestmentProfitabilityDto>();
        decimal totalInitialValue = 0;
        decimal totalCurrentValue = 0;
        DateTime? earliestDate = null;
        DateTime? latestDate = null;

        foreach (var investment in investments)
        {
            InvestmentProfitabilityDto? profitability;

            // Check if investment has transactions
            var hasTransactions = investment.InvestmentTransactions.Any();

            if (hasTransactions)
            {
                // Use transaction-based calculation
                profitability = CalculateProfitabilityWithTransactions(investment);
            }
            else
            {
                // Use simple calculation with date filtering
                var history = investment.InvestmentHistories
                    .Where(h => (!startDate.HasValue || h.RecordedDate >= startDate.Value) &&
                               (!endDate.HasValue || h.RecordedDate <= endDate.Value))
                    .OrderBy(h => h.RecordedDate)
                    .ToList();

                if (history.Count == 0)
                {
                    // No history in this date range, skip
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

                profitability = new InvestmentProfitabilityDto
                {
                    InvestmentId = investment.Id,
                    InvestmentName = investment.Name,
                    InvestmentTypeName = investment.InvestmentType?.Name ?? "Unknown",
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

            if (profitability != null)
            {
                investmentProfitabilities.Add(profitability);

                totalInitialValue += profitability.InitialValue;
                totalCurrentValue += profitability.CurrentValue;

                if (!earliestDate.HasValue || profitability.FirstRecordedDate < earliestDate.Value)
                    earliestDate = profitability.FirstRecordedDate;
                if (!latestDate.HasValue || profitability.LastRecordedDate > latestDate.Value)
                    latestDate = profitability.LastRecordedDate;
            }
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
