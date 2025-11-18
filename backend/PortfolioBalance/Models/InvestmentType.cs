namespace PortfolioBalance.Models;

public class InvestmentType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal AllocationPercentage { get; set; }
    public ICollection<Investment> Investments { get; set; } = new List<Investment>();
}
