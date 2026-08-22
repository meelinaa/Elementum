namespace Elementum.Application.DTOs;

public record LiveMarketOverviewDto
{
    public List<LiveMetalPriceDto> Items { get; init; } = [];
    public decimal ExchangeRateUsdEur { get; init; }
    public long Timestamp { get; init; }
    public DateTime LastUpdatedAtLocal { get; init; }
}
