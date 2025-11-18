namespace PortfolioBalance.DTOs;

public class InvestmentTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal AllocationPercentage { get; set; }
    public decimal CurrentTotalValue { get; set; }
}

public class UpdateAllocationDto
{
    public int Id { get; set; }
    public decimal AllocationPercentage { get; set; }
}
