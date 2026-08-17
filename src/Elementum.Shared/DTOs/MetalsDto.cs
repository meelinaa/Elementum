namespace Elementum.Shared.DTOs;

/// <summary>
/// Slim DTO for metal list/detail API responses. Excludes internal fields like CreatedAt.
/// </summary>
public record MetalsDto
{
    public int Id { get; init; }
    public string Symbol { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}
