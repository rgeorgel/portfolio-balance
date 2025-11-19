namespace PortfolioBalance.Models;

public class UserInvestmentTypeAllocation
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int InvestmentTypeId { get; set; }
    public decimal AllocationPercentage { get; set; }

    public User? User { get; set; }
    public InvestmentType? InvestmentType { get; set; }
}
