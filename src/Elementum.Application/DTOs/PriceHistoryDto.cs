namespace Elementum.Application.DTOs;

/// <summary>
/// DTO representing a historical price tick record.
/// </summary>
public record PriceHistoryDto
{
    public int Id { get; init; }
    public int MetalId { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Symbol { get; init; } = string.Empty;
    public long ReferenceTimestamp { get; init; }
    public DateOnly EntryDate { get; init; }
    public decimal Price { get; init; }
    public decimal? Chp { get; init; }
    public MetalsDto? Metal { get; init; }
}
