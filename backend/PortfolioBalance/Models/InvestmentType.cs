namespace PortfolioBalance.Models;

public class InvestmentType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Investment> Investments { get; set; } = new List<Investment>();
    public ICollection<UserInvestmentTypeAllocation> UserAllocations { get; set; } = new List<UserInvestmentTypeAllocation>();
}
