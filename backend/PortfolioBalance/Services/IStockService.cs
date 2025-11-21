using PortfolioBalance.DTOs;

namespace PortfolioBalance.Services
{
    public interface IStockService
    {
        Task<StockQuoteDto?> GetStockQuoteAsync(string ticker);
    }
}
