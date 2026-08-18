namespace Elementum.Application.DTOs;

/// <summary>
/// DTO representing technical trading indicators and market analysis for a metal.
/// </summary>
public record TradingPriceDto
{
    public int Id { get; init; }
    public string Symbol { get; init; } = string.Empty;
    public string MetalName { get; init; } = string.Empty;
    public string Exchange { get; init; } = "EDELMETALLE";
    public string Currency { get; init; } = "EUR";
    public DateOnly EntryDate { get; init; }
    public long? ReferenceTimestamp { get; init; }
    public decimal Price { get; init; }
    public decimal? PrevClosePrice { get; init; }
    public decimal? OpenPrice { get; init; }
    public decimal? LowPrice { get; init; }
    public decimal? HighPrice { get; init; }
    public decimal? Ch { get; init; }
    public decimal? Chp { get; init; }
    public decimal? DifferencePrevClose { get; init; }
    public decimal? VolatilityRange { get; init; }
    public decimal? VolatilityPercent { get; init; }
    public string Status { get; init; } = "NEUTRAL";
    public decimal? ExchangeRateUsdEur { get; init; }
}
