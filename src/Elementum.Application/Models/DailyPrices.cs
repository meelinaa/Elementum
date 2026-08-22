using System.Text.Json.Serialization;

namespace Elementum.Application.Models;

/// <summary>
/// Model for daily price responses from external metals API (e.g. GoldAPI).
/// </summary>
public record DailyPrices
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("metal")]
    public string Metal { get; init; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; init; } = string.Empty;

    [JsonPropertyName("exchange")]
    public string Exchange { get; init; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; init; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; init; }

    [JsonPropertyName("open_time")]
    public long OpenTime { get; init; }

    [JsonPropertyName("prev_close_price")]
    public decimal PrevClosePrice { get; init; }

    [JsonPropertyName("open_price")]
    public decimal OpenPrice { get; init; }

    [JsonPropertyName("low_price")]
    public decimal LowPrice { get; init; }

    [JsonPropertyName("high_price")]
    public decimal HighPrice { get; init; }

    [JsonPropertyName("price")]
    public decimal Price { get; init; }

    [JsonPropertyName("ch")]
    public decimal? Ch { get; init; }

    [JsonPropertyName("chp")]
    public decimal? Chp { get; init; }

    [JsonPropertyName("ask")]
    public decimal? Ask { get; init; }

    [JsonPropertyName("bid")]
    public decimal? Bid { get; init; }

    [JsonPropertyName("price_gram_24k")]
    public decimal? PriceGram24k { get; init; }

    [JsonPropertyName("price_gram_22k")]
    public decimal? PriceGram22k { get; init; }

    [JsonPropertyName("price_gram_21k")]
    public decimal? PriceGram21k { get; init; }

    [JsonPropertyName("price_gram_20k")]
    public decimal? PriceGram20k { get; init; }

    [JsonPropertyName("price_gram_18k")]
    public decimal? PriceGram18k { get; init; }

    [JsonPropertyName("price_gram_16k")]
    public decimal? PriceGram16k { get; init; }

    [JsonPropertyName("price_gram_14k")]
    public decimal? PriceGram14k { get; init; }

    [JsonPropertyName("price_gram_10k")]
    public decimal? PriceGram10k { get; init; }
}
