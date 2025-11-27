using System.ComponentModel.DataAnnotations;
using PortfolioBalance.Models;

namespace PortfolioBalance.DTOs;

public class InvestmentTransactionDto
{
    public int Id { get; set; }
    public int InvestmentId { get; set; }
    public TransactionType Type { get; set; }
    public string TypeName => Type.ToString();
    public decimal Amount { get; set; }
    public decimal? UnitValue { get; set; }
    public decimal? Quantity { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Notes { get; set; }
    public string? InvestmentName { get; set; }
}

public class CreateInvestmentTransactionDto
{
    [Required]
    public int InvestmentId { get; set; }

    [Required]
    public TransactionType Type { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? UnitValue { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Quantity { get; set; }

    public DateTime? TransactionDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class UpdateInvestmentTransactionDto
{
    [Required]
    public TransactionType Type { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? UnitValue { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Quantity { get; set; }

    public DateTime? TransactionDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
