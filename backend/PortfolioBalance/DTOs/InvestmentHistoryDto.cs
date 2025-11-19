using System.ComponentModel.DataAnnotations;

namespace PortfolioBalance.DTOs;

public class InvestmentHistoryDto
{
    public int Id { get; set; }
    public int InvestmentId { get; set; }
    public decimal Value { get; set; }
    public decimal? UnitValue { get; set; }
    public decimal? Quantity { get; set; }
    public DateTime RecordedDate { get; set; }
    public string? Notes { get; set; }
    public string? InvestmentName { get; set; }
}

public class CreateInvestmentHistoryDto
{
    [Required]
    public int InvestmentId { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Value { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? UnitValue { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Quantity { get; set; }

    public DateTime? RecordedDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class PortfolioHistoryDto
{
    public DateTime Date { get; set; }
    public decimal TotalValue { get; set; }
    public Dictionary<string, decimal> ValueByType { get; set; } = new();
}
