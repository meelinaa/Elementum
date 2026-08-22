namespace Elementum.Application.DTOs;

public record LiveMetalPriceDto
{
    public string Symbol { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal PriceUsd { get; init; }
    public decimal PriceEur { get; init; }
    public decimal? OpenPriceUsd { get; init; }
    public decimal? OpenPriceEur { get; init; }
    public decimal? ChpUsd { get; init; }
    public decimal? ChpEur { get; init; }
    public decimal? HighPriceUsd { get; init; }
    public decimal? HighPriceEur { get; init; }
    public decimal? LowPriceUsd { get; init; }
    public decimal? LowPriceEur { get; init; }
    public decimal? PrevCloseUsd { get; init; }
    public decimal? PrevCloseEur { get; init; }
}
