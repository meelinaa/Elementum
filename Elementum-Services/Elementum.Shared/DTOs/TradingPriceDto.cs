namespace Elementum.Shared.DTOs;

/// <summary>
/// DTO with trading-relevant fields (bid/ask, high/low, open, change) plus Id and timestamps for the CLI trading view.
/// Use when the client needs trading data (e.g. GET history/{symbol}/latest/trading).
/// </summary>
public class TradingPriceDto
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string MetalName { get; set; } = string.Empty;
    public string? Exchange { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateOnly EntryDate { get; set; }
    public long? ReferenceTimestamp { get; set; }
    public long? OpenTime { get; set; }

    public decimal Price { get; set; }
    public decimal? PrevClosePrice { get; set; }
    public decimal? OpenPrice { get; set; }
    public decimal? LowPrice { get; set; }
    public decimal? HighPrice { get; set; }
    public decimal? Ch { get; set; }
    public decimal? Chp { get; set; }
    public decimal? Ask { get; set; }
    public decimal? Bid { get; set; }
}
