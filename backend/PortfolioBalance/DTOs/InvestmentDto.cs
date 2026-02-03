namespace PortfolioBalance.DTOs;

public class InvestmentDto
{
    public int Id { get; set; }
    public int InvestmentTypeId { get; set; }
    public string InvestmentTypeName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal? UnitValue { get; set; }
    public decimal? Quantity { get; set; }
    public decimal Weight { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastUpdatedDate { get; set; }
}

public class CreateInvestmentDto
{
    public int InvestmentTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal? UnitValue { get; set; }
    public decimal? Quantity { get; set; }
    public decimal Weight { get; set; } = 1.0m;
    public DateTime? CreatedDate { get; set; }
}

public class UpdateInvestmentDto
{
    public string Name { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal? UnitValue { get; set; }
    public decimal? Quantity { get; set; }
    public decimal Weight { get; set; }
}
