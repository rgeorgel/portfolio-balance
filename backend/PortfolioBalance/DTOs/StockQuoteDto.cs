namespace PortfolioBalance.DTOs
{
    public class StockQuoteDto
    {
        public string Symbol { get; set; } = string.Empty;
        public string LongName { get; set; } = string.Empty;
        public decimal RegularMarketPrice { get; set; }
        public decimal RegularMarketChange { get; set; }
        public decimal RegularMarketChangePercent { get; set; }
        public DateTime RegularMarketTime { get; set; }
        public string Currency { get; set; } = "BRL";
    }
}
