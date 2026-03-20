namespace Elementum.Shared.DTOs;

/// <summary>
/// Slim DTO for price history when full entity is not needed. Excludes OHLC, karat prices, ask/bid, etc.
/// Use for list/history endpoints where only core price + metal info is required.
/// </summary>
public record PriceHistoryDto
{
    public long Id { get; init; }
    public int MetalId { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string? Exchange { get; init; }
    public string? Symbol { get; init; }

    public long? ReferenceTimestamp { get; init; }
    public DateOnly EntryDate { get; init; }

    public decimal Price { get; init; }
    public decimal? Chp { get; init; }
    public MetalsDto? Metal { get; init; }
}
