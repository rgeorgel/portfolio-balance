using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;
        private static readonly string[] FreeTierTickers = { "PETR4", "MGLU3", "VALE3", "ITUB4" };

        public StockQuotesController(IStockService stockService, ILogger<StockQuotesController> logger, IConfiguration configuration)
        {
            _stockService = stockService;
            _logger = logger;
            _configuration = configuration;
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

            var normalizedTicker = ticker.Replace(".SA", "").ToUpper();
            var quote = await _stockService.GetStockQuoteAsync(ticker);

            if (quote == null)
            {
                // Check if API token is configured
                var apiToken = _configuration["Brapi:ApiToken"];
                var hasToken = !string.IsNullOrWhiteSpace(apiToken);
                var isFreeTicker = Array.Exists(FreeTierTickers, t => t.Equals(normalizedTicker, StringComparison.OrdinalIgnoreCase));

                // Provide helpful error message
                if (!hasToken && !isFreeTicker)
                {
                    return StatusCode(402, new
                    {
                        error = "API token required",
                        message = $"A API Brapi requer um token para consultar {normalizedTicker}. Apenas {string.Join(", ", FreeTierTickers)} estão disponíveis sem token. Configure um token em appsettings.json ou use uma das ações gratuitas.",
                        freeTickers = FreeTierTickers
                    });
                }

                return NotFound(new
                {
                    error = "Stock not found",
                    message = $"Cotação não encontrada para o ticker: {normalizedTicker}. Verifique se o código está correto."
                });
            }

            return Ok(quote);
        }
    }
}
