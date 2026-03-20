namespace Elementum.Shared.Objects;

/// <summary>
/// Entity for the price_history table (one row per metal/currency/day).
/// </summary>
public record PriceHistory
{
    public long Id { get; init; }
    public int MetalId { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string? Exchange { get; init; }
    public string? Symbol { get; init; }

    public long? ReferenceTimestamp { get; init; }
    public long? OpenTime { get; init; }
    public DateOnly EntryDate { get; init; }

    public decimal Price { get; init; }
    public decimal? PrevClosePrice { get; init; }
    public decimal? OpenPrice { get; init; }
    public decimal? LowPrice { get; init; }
    public decimal? HighPrice { get; init; }
    public decimal? Ch { get; init; }
    public decimal? Chp { get; init; }
    public decimal? Ask { get; init; }
    public decimal? Bid { get; init; }

    public decimal? PriceGram24k { get; init; }
    public decimal? PriceGram22k { get; init; }
    public decimal? PriceGram21k { get; init; }
    public decimal? PriceGram20k { get; init; }
    public decimal? PriceGram18k { get; init; }
    public decimal? PriceGram16k { get; init; }
    public decimal? PriceGram14k { get; init; }
    public decimal? PriceGram10k { get; init; }

    public Metals? Metal { get; init; }
}
