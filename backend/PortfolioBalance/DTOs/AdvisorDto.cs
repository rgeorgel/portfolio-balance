using System.ComponentModel.DataAnnotations;

namespace PortfolioBalance.DTOs;

public class AdvisorRegisterDto
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [StringLength(200)]
    public string? FullName { get; set; }
}

public class AdvisorLoginDto
{
    [Required]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AdvisorAuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public bool IsAdvisor { get; set; } = true;
}

public class AdvisorClientDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime AssignedDate { get; set; }
    public bool IsActive { get; set; }
    public decimal TotalPortfolioValue { get; set; }
    public int TotalInvestments { get; set; }
}

public class AddClientDto
{
    [Required]
    public string UsernameOrEmail { get; set; } = string.Empty;
}

public class ClientPortfolioSummaryDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
    public int TotalInvestments { get; set; }
    public List<InvestmentTypeDto> InvestmentTypes { get; set; } = new();
    public List<InvestmentDto> Investments { get; set; } = new();
}
