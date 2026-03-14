namespace Elementum.Shared.DTOs;

/// <summary>
/// DTO containing only price-per-gram by purity (karat). For karat calculator / alloy views.
/// Use when the client needs only gram prices (e.g. GET history/{symbol}/latest/karat).
/// </summary>
public class KaratPricesDto
{
    public string Symbol { get; set; } = string.Empty;
    public string MetalName { get; set; } = string.Empty;
    public DateOnly EntryDate { get; set; }
    public string Currency { get; set; } = string.Empty;

    public decimal? PriceGram24k { get; set; }
    public decimal? PriceGram22k { get; set; }
    public decimal? PriceGram21k { get; set; }
    public decimal? PriceGram20k { get; set; }
    public decimal? PriceGram18k { get; set; }
    public decimal? PriceGram16k { get; set; }
    public decimal? PriceGram14k { get; set; }
    public decimal? PriceGram10k { get; set; }
}
