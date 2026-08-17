namespace Elementum.Domain.Entities;

public class PriceHistory
{
    public int Id { get; set; }
    public int MetalId { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string ReferenceTimestamp { get; set; } = string.Empty;
    public DateOnly EntryDate { get; set; }
    public decimal Price { get; set; }
    public decimal? PrevClosePrice { get; set; }
    public decimal? OpenPrice { get; set; }
    public decimal? LowPrice { get; set; }
    public decimal? HighPrice { get; set; }
    public string OpenTime { get; set; } = string.Empty;
    public decimal? Ch { get; set; }
    public decimal? Chp { get; set; }
    public decimal? Ask { get; set; }
    public decimal? Bid { get; set; }
    public decimal? PriceGram24k { get; set; }
    public decimal? PriceGram22k { get; set; }
    public decimal? PriceGram21k { get; set; }
    public decimal? PriceGram20k { get; set; }
    public decimal? PriceGram18k { get; set; }
    public decimal? PriceGram16k { get; set; }
    public decimal? PriceGram14k { get; set; }
    public decimal? PriceGram10k { get; set; }

    public Metals? Metal { get; set; }
}
