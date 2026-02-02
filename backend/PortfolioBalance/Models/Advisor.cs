namespace PortfolioBalance.Models;

public class Advisor
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public ICollection<AdvisorClient> Clients { get; set; } = new List<AdvisorClient>();
}
