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
    /// Uses Money-Weighted Return approach.
    ///
    /// Handles two regimes:
    ///   * Open position  – net invested (initial + deposits - withdrawals) > 0 and there are still
    ///                     units remaining. InitialValue in the DTO equals net invested; CurrentValue
    ///                     is the latest market value; % is relative to net invested.
    ///   * Fully liquidated – net invested &lt;= 0 or no units left. InitialValue in the DTO equals
    ///                     the gross amount that went in (never negative); CurrentValue is 0;
    ///                     the absolute return is realized (withdrawals + dividends - gross in).
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

        // Determine the start date (earliest between first transaction and first history)
        var firstDate = firstTransaction.TransactionDate;
        if (history.Any() && history.First().RecordedDate < firstDate)
        {
            firstDate = history.First().RecordedDate;
        }

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

        // Add initial investment value from first history record to total deposits
        // This represents the initial investment that may not have a transaction record
        decimal initialInvestmentValue = 0;
        if (history.Any())
        {
            var firstHistoryRecord = history.First();
            // Only include initial value if it's before or at the same time as the first deposit transaction
            if (firstHistoryRecord.RecordedDate <= firstTransaction.TransactionDate)
            {
                initialInvestmentValue = firstHistoryRecord.Value;
            }
        }

        decimal grossInvested = initialInvestmentValue + totalDeposits;
        decimal netInvested = grossInvested - totalWithdrawals;

        // Detect whether the position has been fully liquidated. We consider it closed when
        // the net invested amount has been driven to zero or below by withdrawals, or when
        // the investment record itself shows no units / no market value left.
        bool noUnits = !investment.Quantity.HasValue || investment.Quantity.Value <= 0;
        bool fullyLiquidated = noUnits || netInvested <= 0;

        if (fullyLiquidated)
        {
            // Position closed: realized P/L is everything that came out (withdrawals + dividends)
            // minus everything that went in (initial + deposits). Use gross invested as the
            // denominator for the percentage so the sign of the return is preserved.
            decimal realizedReturn = (totalWithdrawals + totalDividends) - grossInvested;
            decimal liquidatedReturnPercentage = grossInvested > 0
                ? (realizedReturn / grossInvested) * 100
                : 0;

            var periodDays = (int)(lastDate - firstDate).TotalDays;

            return new InvestmentProfitabilityDto
            {
                InvestmentId = investment.Id,
                InvestmentName = investment.Name,
                InvestmentTypeName = investment.InvestmentType?.Name ?? "Unknown",
                InitialValue = grossInvested,
                CurrentValue = 0,
                AbsoluteReturn = realizedReturn,
                ReturnPercentage = liquidatedReturnPercentage,
                AnnualizedReturn = 0, // Position is closed, annualization is not meaningful
                InvestmentPeriodDays = periodDays,
                FirstRecordedDate = firstDate,
                LastRecordedDate = lastDate
            };
        }

        // Open position: use latest market value as current value.
        decimal currentValue = history.Any() ? history.Last().Value : investment.CurrentValue;

        // Money-weighted absolute return: money out (current value + withdrawals + dividends)
        // minus money in (initial + deposits). Equivalent to (currentValue - netInvested) + dividends.
        decimal absoluteReturn = (currentValue + totalWithdrawals + totalDividends) - grossInvested;

        decimal returnPercentage = netInvested > 0
            ? (absoluteReturn / netInvested) * 100
            : 0;

        var openPeriodDays = (int)(lastDate - firstDate).TotalDays;
        var annualizedReturn = CalculateAnnualizedReturn(netInvested, currentValue, openPeriodDays);

        return new InvestmentProfitabilityDto
        {
            InvestmentId = investment.Id,
            InvestmentName = investment.Name,
            InvestmentTypeName = investment.InvestmentType?.Name ?? "Unknown",
            InitialValue = netInvested,
            CurrentValue = currentValue,
            AbsoluteReturn = absoluteReturn,
            ReturnPercentage = returnPercentage,
            AnnualizedReturn = annualizedReturn,
            InvestmentPeriodDays = openPeriodDays,
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

        // Sum per-investment absolute returns instead of recomputing from totals.
        // The simple (totalCurrentValue - totalInitialValue) formula ignores withdrawals
        // and dividends on open positions, and gives a nonsensical negative for fully
        // liquidated ones. The per-investment DTO already has the correct money-weighted
        // absolute return for both regimes.
        var totalAbsoluteReturn = investmentProfitabilities.Sum(p => p.AbsoluteReturn);
        var totalReturnPercentage = totalInitialValue > 0
            ? (totalAbsoluteReturn / totalInitialValue) * 100
            : 0;
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
