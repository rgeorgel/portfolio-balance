namespace PortfolioBalance.DTOs;

/// <summary>
/// Profitability metrics for a single investment
/// </summary>
public class InvestmentProfitabilityDto
{
    public int InvestmentId { get; set; }
    public string InvestmentName { get; set; } = string.Empty;
    public string InvestmentTypeName { get; set; } = string.Empty;
    public decimal InitialValue { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal AbsoluteReturn { get; set; }
    public decimal ReturnPercentage { get; set; }
    public decimal AnnualizedReturn { get; set; }
    public int InvestmentPeriodDays { get; set; }
    public DateTime FirstRecordedDate { get; set; }
    public DateTime LastRecordedDate { get; set; }
}

/// <summary>
/// Profitability metrics grouped by investment type
/// </summary>
public class InvestmentTypeProfitabilityDto
{
    public int InvestmentTypeId { get; set; }
    public string InvestmentTypeName { get; set; } = string.Empty;
    public decimal InitialValue { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal AbsoluteReturn { get; set; }
    public decimal ReturnPercentage { get; set; }
    public decimal AnnualizedReturn { get; set; }
    public int InvestmentCount { get; set; }
}

/// <summary>
/// Overall portfolio profitability metrics
/// </summary>
public class PortfolioProfitabilityDto
{
    public decimal TotalInitialValue { get; set; }
    public decimal TotalCurrentValue { get; set; }
    public decimal TotalAbsoluteReturn { get; set; }
    public decimal TotalReturnPercentage { get; set; }
    public decimal TotalAnnualizedReturn { get; set; }
    public int TotalInvestments { get; set; }
    public DateTime? EarliestInvestmentDate { get; set; }
    public DateTime? LatestUpdateDate { get; set; }
    public List<InvestmentProfitabilityDto> InvestmentsByPerformance { get; set; } = new();
    public List<InvestmentTypeProfitabilityDto> ProfitabilityByType { get; set; } = new();
}

/// <summary>
/// Top performers response
/// </summary>
public class TopPerformersDto
{
    public List<InvestmentProfitabilityDto> TopPerformers { get; set; } = new();
    public List<InvestmentProfitabilityDto> BottomPerformers { get; set; } = new();
}
