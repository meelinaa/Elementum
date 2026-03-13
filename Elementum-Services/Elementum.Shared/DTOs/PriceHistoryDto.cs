namespace Elementum.Shared.DTOs;

/// <summary>
/// Slim DTO for price history when full entity is not needed. Excludes OHLC, karat prices, ask/bid, etc.
/// Use for list/history endpoints where only core price + metal info is required.
/// </summary>
public class PriceHistoryDto
{
    public long Id { get; set; }
    public int MetalId { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Exchange { get; set; }
    public string? Symbol { get; set; }

    public long? ReferenceTimestamp { get; set; }
    public DateOnly EntryDate { get; set; }

    public decimal Price { get; set; }
    public decimal? Chp { get; set; }
    public MetalsDto? Metal { get; set; }
}

