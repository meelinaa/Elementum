namespace Elementum.Application.DTOs;

/// <summary>
/// Bounded history page: items plus pagination metadata so clients can continue with <c>skip</c>/<c>take</c>.
/// </summary>
public record PriceHistoryPageDto
{
    public IReadOnlyList<PriceHistoryDto> Items { get; init; } = [];
    public int Skip { get; init; }
    public int Take { get; init; }
    public int TotalCount { get; init; }
    public bool HasMore { get; init; }
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
}
