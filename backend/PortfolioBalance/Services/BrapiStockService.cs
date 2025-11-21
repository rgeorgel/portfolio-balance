using System.Text.Json;
using PortfolioBalance.DTOs;

namespace PortfolioBalance.Services
{
    public class BrapiStockService : IStockService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BrapiStockService> _logger;
        private const string BrapiBaseUrl = "https://brapi.dev/api";

        public BrapiStockService(HttpClient httpClient, ILogger<BrapiStockService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<StockQuoteDto?> GetStockQuoteAsync(string ticker)
        {
            try
            {
                // Brapi expects tickers without .SA suffix, but we'll normalize it
                var normalizedTicker = ticker.Replace(".SA", "").ToUpper();

                var response = await _httpClient.GetAsync($"{BrapiBaseUrl}/quote/{normalizedTicker}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to fetch stock quote for {ticker}. Status: {response.StatusCode}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var brapiResponse = JsonSerializer.Deserialize<BrapiResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (brapiResponse?.Results == null || brapiResponse.Results.Length == 0)
                {
                    _logger.LogWarning($"No results found for ticker {ticker}");
                    return null;
                }

                var result = brapiResponse.Results[0];

                return new StockQuoteDto
                {
                    Symbol = result.Symbol ?? normalizedTicker,
                    LongName = result.LongName ?? result.ShortName ?? normalizedTicker,
                    RegularMarketPrice = result.RegularMarketPrice,
                    RegularMarketChange = result.RegularMarketChange,
                    RegularMarketChangePercent = result.RegularMarketChangePercent,
                    RegularMarketTime = DateTimeOffset.FromUnixTimeSeconds(result.RegularMarketTime).DateTime,
                    Currency = result.Currency ?? "BRL"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching stock quote for {ticker}");
                return null;
            }
        }

        // Internal classes for deserializing Brapi response
        private class BrapiResponse
        {
            public BrapiResult[]? Results { get; set; }
        }

        private class BrapiResult
        {
            public string? Symbol { get; set; }
            public string? ShortName { get; set; }
            public string? LongName { get; set; }
            public decimal RegularMarketPrice { get; set; }
            public decimal RegularMarketChange { get; set; }
            public decimal RegularMarketChangePercent { get; set; }
            public long RegularMarketTime { get; set; }
            public string? Currency { get; set; }
        }
    }
}
