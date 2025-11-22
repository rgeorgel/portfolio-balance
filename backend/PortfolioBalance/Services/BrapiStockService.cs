using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PortfolioBalance.DTOs;

namespace PortfolioBalance.Services
{
    public class BrapiStockService : IStockService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BrapiStockService> _logger;
        private readonly IConfiguration _configuration;
        private const string BrapiBaseUrl = "https://brapi.dev/api";
        private static readonly string[] FreeTierTickers = { "PETR4", "MGLU3", "VALE3", "ITUB4" };

        public BrapiStockService(HttpClient httpClient, ILogger<BrapiStockService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<StockQuoteDto?> GetStockQuoteAsync(string ticker)
        {
            try
            {
                // Brapi expects tickers without .SA suffix, but we'll normalize it
                var normalizedTicker = ticker.Replace(".SA", "").ToUpper();

                // Get API token from configuration (optional)
                var apiToken = _configuration["Brapi:ApiToken"];
                var hasToken = !string.IsNullOrWhiteSpace(apiToken);

                // Build request URL
                var url = $"{BrapiBaseUrl}/quote/{normalizedTicker}";
                if (hasToken)
                {
                    url += $"?token={apiToken}";
                }

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    // Provide helpful error message for 401/403 errors
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                        response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        var isFreeTicker = Array.Exists(FreeTierTickers, t => t.Equals(normalizedTicker, StringComparison.OrdinalIgnoreCase));

                        if (!hasToken && !isFreeTicker)
                        {
                            _logger.LogWarning($"API token required for {ticker}. Free tier only supports: {string.Join(", ", FreeTierTickers)}");
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to fetch stock quote for {ticker}. Invalid or expired API token. Status: {response.StatusCode}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to fetch stock quote for {ticker}. Status: {response.StatusCode}");
                    }
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
