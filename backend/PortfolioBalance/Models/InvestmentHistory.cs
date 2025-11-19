namespace PortfolioBalance.Models;

public class InvestmentHistory
{
    public int Id { get; set; }
    public int InvestmentId { get; set; }
    public decimal Value { get; set; }
    public decimal? UnitValue { get; set; }
    public decimal? Quantity { get; set; }
    public DateTime RecordedDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public Investment? Investment { get; set; }
}
