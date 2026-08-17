namespace Elementum.Shared.DTOs;

/// <summary>
/// DTO with trading-relevant fields (bid/ask, high/low, open, change) plus Id and timestamps for the CLI trading view.
/// Use when the client needs trading data (e.g. GET history/{symbol}/latest/trading).
/// </summary>
public record TradingPriceDto
{
    public long Id { get; init; }
    public string Symbol { get; init; } = string.Empty;
    public string MetalName { get; init; } = string.Empty;
    public string? Exchange { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateOnly EntryDate { get; init; }
    public long? ReferenceTimestamp { get; init; }
    public long? OpenTime { get; init; }

    public decimal Price { get; init; }
    public decimal? PrevClosePrice { get; init; }
    public decimal? OpenPrice { get; init; }
    public decimal? LowPrice { get; init; }
    public decimal? HighPrice { get; init; }
    public decimal? Ch { get; init; }
    public decimal? Chp { get; init; }
    public decimal? Ask { get; init; }
    public decimal? Bid { get; init; }
}
