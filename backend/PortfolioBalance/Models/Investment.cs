namespace PortfolioBalance.Models;

public class Investment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int InvestmentTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal? UnitValue { get; set; }
    public decimal? Quantity { get; set; }
    public decimal Weight { get; set; } = 1.0m;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedDate { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public InvestmentType? InvestmentType { get; set; }
    public ICollection<InvestmentHistory> InvestmentHistories { get; set; } = new List<InvestmentHistory>();
    public ICollection<InvestmentTransaction> InvestmentTransactions { get; set; } = new List<InvestmentTransaction>();
}
