namespace PortfolioBalance.Models;

public class Investment
{
    public int Id { get; set; }
    public int InvestmentTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal Weight { get; set; } = 1.0m;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public InvestmentType? InvestmentType { get; set; }
}
