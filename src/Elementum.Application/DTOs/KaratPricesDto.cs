namespace Elementum.Application.DTOs;

public record KaratPricesDto
{
    public string Symbol { get; init; } = string.Empty;
    public string MetalName { get; init; } = string.Empty;
    public DateOnly EntryDate { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal? PriceGram24k { get; init; }
    public decimal? PriceGram22k { get; init; }
    public decimal? PriceGram21k { get; init; }
    public decimal? PriceGram20k { get; init; }
    public decimal? PriceGram18k { get; init; }
    public decimal? PriceGram16k { get; init; }
    public decimal? PriceGram14k { get; init; }
    public decimal? PriceGram10k { get; init; }
}
