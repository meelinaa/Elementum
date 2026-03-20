namespace Elementum.Shared.Objects;

/// <summary>
/// Entity for the <c>metals</c> table (Gold, Silver, Platinum). Used by EF Core and mapped to <see cref="DTOs.MetalsDto"/> for API responses.
/// </summary>
public record Metals
{
    public int Id { get; init; }
    public string Symbol { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DateTime? CreatedAt { get; init; }
}
