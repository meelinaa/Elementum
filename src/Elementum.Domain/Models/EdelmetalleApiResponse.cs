using System.Text.Json.Serialization;

namespace Elementum.Domain.Models;

/// <summary>
/// Response model for https://api.edelmetalle.de/public.json
/// Contains current price quotes for Gold, Silver, Platinum, and Palladium in USD & EUR, plus exchange rate.
/// </summary>
public record EdelmetalleApiResponse
{
    [JsonPropertyName("gold_usd")]
    public decimal GoldUsd { get; init; }

    [JsonPropertyName("gold_eur")]
    public decimal GoldEur { get; init; }

    [JsonPropertyName("silber_usd")]
    public decimal SilberUsd { get; init; }

    [JsonPropertyName("silber_eur")]
    public decimal SilberEur { get; init; }

    [JsonPropertyName("platin_usd")]
    public decimal PlatinUsd { get; init; }

    [JsonPropertyName("platin_eur")]
    public decimal PlatinEur { get; init; }

    [JsonPropertyName("palladium_usd")]
    public decimal PalladiumUsd { get; init; }

    [JsonPropertyName("palladium_eur")]
    public decimal PalladiumEur { get; init; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; init; }

    [JsonPropertyName("wechselkurs_usd_eur")]
    public decimal WechselkursUsdEur { get; init; }
}
