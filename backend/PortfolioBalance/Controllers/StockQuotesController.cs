using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioBalance.DTOs;
using PortfolioBalance.Services;

namespace PortfolioBalance.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StockQuotesController : ControllerBase
    {
        private readonly IStockService _stockService;
        private readonly ILogger<StockQuotesController> _logger;

        public StockQuotesController(IStockService stockService, ILogger<StockQuotesController> logger)
        {
            _stockService = stockService;
            _logger = logger;
        }

        /// <summary>
        /// Get current stock quote for a Brazilian stock ticker
        /// </summary>
        /// <param name="ticker">Stock ticker symbol (e.g., PETR4, VALE3, ITUB4)</param>
        /// <returns>Stock quote information</returns>
        [HttpGet("{ticker}")]
        public async Task<ActionResult<StockQuoteDto>> GetStockQuote(string ticker)
        {
            if (string.IsNullOrWhiteSpace(ticker))
            {
                return BadRequest("Ticker symbol is required");
            }

            var quote = await _stockService.GetStockQuoteAsync(ticker);

            if (quote == null)
            {
                return NotFound($"Stock quote not found for ticker: {ticker}");
            }

            return Ok(quote);
        }
    }
}
