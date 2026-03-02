using System.Text.Json.Serialization;

namespace Elementum.Shared.Objects
{
    public class DailyPrices
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("metal")]
        public string Metal { get; set; } = string.Empty;

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("exchange")]
        public string Exchange { get; set; } = string.Empty;

        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = string.Empty;

        // API timestamps
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("open_time")]
        public long OpenTime { get; set; }

        [JsonPropertyName("prev_close_price")]
        public decimal PrevClosePrice { get; set; }

        [JsonPropertyName("open_price")]
        public decimal OpenPrice { get; set; }

        [JsonPropertyName("low_price")]
        public decimal LowPrice { get; set; }

        [JsonPropertyName("high_price")]
        public decimal HighPrice { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("ch")]
        public decimal Ch { get; set; }

        [JsonPropertyName("chp")]
        public decimal Chp { get; set; }

        [JsonPropertyName("ask")]
        public decimal Ask { get; set; }

        [JsonPropertyName("bid")]
        public decimal Bid { get; set; }

        // Price per gram by purity
        [JsonPropertyName("price_gram_24k")]
        public decimal PriceGram24k { get; set; }

        [JsonPropertyName("price_gram_22k")]
        public decimal PriceGram22k { get; set; }

        [JsonPropertyName("price_gram_21k")]
        public decimal PriceGram21k { get; set; }

        [JsonPropertyName("price_gram_20k")]
        public decimal PriceGram20k { get; set; }

        [JsonPropertyName("price_gram_18k")]
        public decimal PriceGram18k { get; set; }

        [JsonPropertyName("price_gram_16k")]
        public decimal PriceGram16k { get; set; }

        [JsonPropertyName("price_gram_14k")]
        public decimal PriceGram14k { get; set; }

        [JsonPropertyName("price_gram_10k")]
        public decimal PriceGram10k { get; set; }
    }
}
