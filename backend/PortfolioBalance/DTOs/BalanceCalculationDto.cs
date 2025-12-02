namespace PortfolioBalance.DTOs;

public class BalanceCalculationRequestDto
{
    public decimal NewInvestmentAmount { get; set; }
}

public class BalanceCalculationResponseDto
{
    public decimal TotalPortfolioValue { get; set; }
    public decimal NewInvestmentAmount { get; set; }
    public decimal TotalAfterInvestment { get; set; }
    public List<TypeAllocationDto> Allocations { get; set; } = new();
}

public class TypeAllocationDto
{
    public int InvestmentTypeId { get; set; }
    public string InvestmentTypeName { get; set; } = string.Empty;
    public decimal TargetPercentage { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal CurrentPercentage { get; set; }
    public decimal AmountToInvest { get; set; }
    public decimal ValueAfterInvestment { get; set; }
    public decimal PercentageAfterInvestment { get; set; }
    public List<InvestmentAllocationDto> InvestmentAllocations { get; set; } = new();
}

public class InvestmentAllocationDto
{
    public int InvestmentId { get; set; }
    public string InvestmentName { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal AmountToInvest { get; set; }
    public decimal ValueAfterInvestment { get; set; }
    public decimal? UnitValue { get; set; }
    public decimal? Quantity { get; set; }
}
