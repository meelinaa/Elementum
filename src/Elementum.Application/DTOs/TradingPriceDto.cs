namespace Elementum.Application.DTOs;

public record TradingPriceDto
{
    public int Id { get; init; }
    public string Symbol { get; init; } = string.Empty;
    public string MetalName { get; init; } = string.Empty;
    public string Exchange { get; init; } = string.Empty;
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
