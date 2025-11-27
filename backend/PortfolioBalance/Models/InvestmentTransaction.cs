namespace PortfolioBalance.Models;

public class InvestmentTransaction
{
    public int Id { get; set; }
    public int InvestmentId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal? UnitValue { get; set; }
    public decimal? Quantity { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public Investment? Investment { get; set; }
}

public enum TransactionType
{
    Deposit = 1,      // Aporte/Compra
    Withdrawal = 2,   // Resgate/Venda
    Dividend = 3      // Dividendo/Rendimento
}
