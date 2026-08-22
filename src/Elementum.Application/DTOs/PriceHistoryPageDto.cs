namespace Elementum.Application.DTOs;

/// <summary>
/// Bounded history page: items plus pagination metadata and HATEOAS navigation links.
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

    /// <summary>HATEOAS relative URI to the next page of results, or null if on the last page.</summary>
    public string? NextPageUrl { get; init; }

    /// <summary>HATEOAS relative URI to the previous page of results, or null if on the first page.</summary>
    public string? PrevPageUrl { get; init; }
}
