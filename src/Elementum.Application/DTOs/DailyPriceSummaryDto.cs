namespace Elementum.Application.DTOs;

/// <summary>
/// DTO representing a daily candle summary (Min, Max, Open, Close at 22:00) for a metal.
/// </summary>
public record DailyPriceSummaryDto
{
    public int Id { get; init; }
    public int MetalId { get; init; }
    public string Symbol { get; init; } = string.Empty;
    public string MetalName { get; init; } = string.Empty;
    public string Currency { get; init; } = "USD";
    public DateOnly EntryDate { get; init; }
    public decimal OpenPrice { get; init; }
    public decimal HighPrice { get; init; }
    public decimal LowPrice { get; init; }
    public decimal ClosePrice { get; init; }
    public decimal? ExchangeRateUsdEur { get; init; }
}
