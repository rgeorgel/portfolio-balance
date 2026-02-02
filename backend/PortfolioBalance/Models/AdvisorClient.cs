namespace PortfolioBalance.Models;

public class AdvisorClient
{
    public int Id { get; set; }
    public int AdvisorId { get; set; }
    public int UserId { get; set; }
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public Advisor Advisor { get; set; } = null!;
    public User User { get; set; } = null!;
}
